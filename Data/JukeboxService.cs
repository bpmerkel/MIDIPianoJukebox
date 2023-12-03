using System.Collections.Concurrent;

namespace MIDIPianoJukebox.Data;

public partial class JukeboxService : IDisposable
{
    const string cxstring = "Filename=jukebox.db";
    private LiteRepository repo = new (cxstring);
    private readonly object syncroot = new();

    public bool Loaded { get; set; } = false;
    public Settings Settings { get; set; } = new();
    public List<Playlist> Playlists { get; private set; }
    public List<Tune> Tunes { get; set; }
    public List<Tune> Queue { get; } = new();
    public static ConcurrentStack<string> Log { get; } = new();
    public Tune Tune { get; set; }
    public int TotalTime { get; set; }
    public TimeSpan CurrentTime { get; set; }
    public TimeSpan RemainingTime { get; set; }
    public MidiPlayer CurrentPlayer { get; set; }
    public PlayerState State => CurrentPlayer?.State ?? PlayerState.Stopped;
    public Action<double> ProgressChanged { get; set; }

    private double _progress = 0d;
    public double Progress
    {
        get => _progress;
        set
        {
            _progress = value;
            ProgressChanged(value);
        }
    }

    public async Task GetJukeboxAsync()
    {
        if (Loaded) return;

        await Task.Run(() =>
        {
            if (Loaded) return;
            lock (syncroot)
            {
                if (Loaded) return;

                var tunes = repo.Database.GetCollection<Tune>();
                var playlists = repo.Database.GetCollection<Playlist>();
                var settings = repo.Database.GetCollection<Settings>();

                tunes.EnsureIndex(x => x.ID, true);
                tunes.EnsureIndex(x => x.Filepath, true);
                playlists.EnsureIndex(x => x.ID, true);
                playlists.EnsureIndex(x => x.Name, true);

                if (playlists.Count() == 0)
                {
                    playlists.Insert(new Playlist { Name = "Favorites", ID = ObjectId.NewObjectId() });
                }

                Settings = settings.FindOne(q => true) ?? new();
                Playlists = playlists.Include(p => p.Tunes).FindAll()
                    .Where(p => p != null)
                    .OrderBy(p => p.Name)
                    .ToList();
                Tunes = tunes.FindAll()
                    .Where(t => t != null)
                    .Where(t => t.Name != null)
                    .Where(t => t.Filepath != null)
                    .Where(t => t.Durationms > 60_000)
                    .Where(t => t.Rating != 1f) // rating of 1 means remove it
                    .OrderBy(t => t.Name)
                    .ToList();

                //AddLog($"Total tunes: {Tunes.Count}; Total playlists: {Playlists.Count}").Start();   //.AndForget();

                // remove tunes from playlists that don't meet the filter rules
                // use a Tune for equality comparison
                Playlists.ForEach(p => p.Tunes = p.Tunes.Intersect(Tunes, new Tune()).ToList());
                Loaded = true;
            }
        });
        return;
    }

    public static async Task AddLog(string msg) => await Task.Run(() => Log.Push($"{DateTime.Now:h:mm:ss}: {msg}")).ConfigureAwait(true);

    public Task RefreshDatabaseAsync(bool doUpdates, Action<string> logger, Action<double> progress)
    {
        return Task.Run(() =>
        {
            logger($"Updating database {cxstring} from MIDI files in {Settings.MIDIPath}");
            var files = new DirectoryInfo(Settings.MIDIPath)
                .EnumerateFiles("*.mid", SearchOption.AllDirectories)
                .Where(fi => fi.Length > 100)
                .OrderBy(fi => fi.Name)
                .ToList();

            var total = files.Count;
            logger($"Processing {total:#,##0} MIDI files");

            // use the top-level folder name as the Genre
            // pre-pend the immediate folder name in the name
            // use the file name as the name
            // capture the file path too

            var re1 = NotAllowed();
            var re2 = ConvertToSpace();
            var reIgnore = Ignore();
            var counter = 0;
            var inserted = 0;
            var updated = 0;
            var sw = Stopwatch.StartNew();
            files.AsParallel().ForAll(file =>
            {
                try
                {
                    Interlocked.Increment(ref counter);
                    if (counter % 1000 == 0 || counter == total)
                    {
                        var rate = counter / (float)sw.ElapsedMilliseconds; // = items per ms
                        var msleft = (total - counter) / rate; // = ms
                        var eta = DateTime.Now.AddMilliseconds(msleft);
                        logger($"{(total - counter):#,##0} {inserted:#,##0} {updated:#,##0} = {(100f * counter / total):##0}% eta: {eta:h:mm:ss} {file.FullName})");
                        progress(100 * counter / total);
                    }

                    var music = GetMusic(file.FullName);
                    if (music == null) return;

                    var duration = music.GetTotalPlayTimeMilliseconds();
                    if (duration < 1000) return;

                    var subpath = file.FullName[(Settings.MIDIPath.Length + 1)..];

                    var tune = repo.Query<Tune>()
                        .Where(t => t.Filepath.Equals(subpath, StringComparison.CurrentCultureIgnoreCase))
                        .FirstOrDefault();
                    if (!doUpdates && tune != null) return;

                    var library = subpath.Contains('\\') ? subpath[..subpath.IndexOf('\\')] : subpath;
                    var name = Path.GetFileNameWithoutExtension(file.Name);
                    var tracksCount = music.Tracks.Count;
                    var messages = music.Tracks.SelectMany(track => track.Messages).ToList();
                    var messagesCount = messages.Count;
                    var events = messages.Select(msg => msg.Event).ToList();
                    var eventsCount = events.Count;
                    var complexity = tracksCount * eventsCount / messagesCount;

                    var tags = new List<string> { library };
                    tags.AddRange(events
                        .Where(e => e.EventType == MidiEvent.Meta && (e.Msb == MidiMetaType.TrackName || e.Msb == MidiMetaType.Text || e.Msb == MidiMetaType.InstrumentName))
                        .Select(e => new string(Encoding.ASCII.GetChars(e.ExtraData)).Trim())
                        .Select(m => re1.Replace(m, string.Empty))
                        .Select(m => re2.Replace(m, " ").Trim())
                        .Where(m => !reIgnore.IsMatch(m))
                    );
                    var tagsForEntity = tags
                        .Where(m => !string.IsNullOrWhiteSpace(m))
                        .Where(m => m.Length > 3)
                        .Select(m => ToTitleCase(m))
                        .OrderBy(m => m)
                        .Distinct()
                        .ToList();

                    // add/update the tune
                    if (tune != null)
                    {
                        tune.Name = name;
                        tune.Library = library;
                        tune.Filepath = subpath;
                        tune.Tracks = tracksCount;
                        tune.Messages = messagesCount;
                        tune.Events = eventsCount;
                        tune.Complexity = complexity;
                        tune.Durationms = duration;
                        tune.Tags = tagsForEntity;
                        // tune.Rating // leave alone
                        // tune.AddedUtc // leave alone
                        repo.Update(tune);
                        updated++;
                    }
                    else
                    {
                        repo.Insert(new Tune
                        {
                            Name = name,
                            Library = library,
                            Filepath = subpath,
                            Tracks = tracksCount,
                            Messages = messagesCount,
                            Events = eventsCount,
                            Complexity = complexity,
                            Durationms = duration,
                            Tags = tagsForEntity,
                            Rating = 0f,
                            AddedUtc = DateTime.UtcNow
                        });
                        inserted++;
                    };
                }
                catch (Exception ex)
                {
                    logger($"Error on {file.FullName}: {ex.Message}");
                    return;
                }
            });

            logger("Shrinking Database...");
            repo.Database.Rebuild();
            logger("Database refresh complete");
        });
    }

    public void SaveSettings()
    {
        repo.Upsert(Settings);
    }

    public void SaveTune(Tune tune)
    {
        repo.Update(tune);
    }

    public void SavePlaylist(Playlist playlist)
    {
        ArgumentNullException.ThrowIfNull(playlist);

        if (playlist.Tunes.Count > 0)
        {
            repo.Upsert(playlist);
        }
        else
        {
            repo.Delete<Playlist>(playlist.ID);
        }
    }

    public void ClearPlaylist(string playlistName)
    {
        if (!string.IsNullOrWhiteSpace(playlistName))
        {
            var playlist = Playlists.FirstOrDefault(p => p.Name.Equals(playlistName, StringComparison.CurrentCultureIgnoreCase));
            if (playlist != null)
            {
                Playlists.Remove(playlist);
                repo.Delete<Playlist>(playlist.ID);
            }
        }
    }

    // get the next Tune from the queue and play it
    public void PlayNext(bool shuffle)
    {
        // interrupt any current playing Tune
        StopPlayer();

        if (Queue.Count != 0)
        {
            // get the next queue item
            var idx = shuffle ? new Random().Next(0, Queue.Count - 1) : 0;
            Tune = Queue[idx];
            Queue.RemoveAt(idx);

            // play 'next'
            PlayPlayer();
        }
        else
        {
            Tune = null;
        }
    }

    public void Enqueue(Tune tune)
    {
        if (tune != null && !Queue.Contains(tune))
        {
            Queue.Add(tune);
        }
    }

    public void Enqueue(Playlist playlist)
    {
        if (playlist != null && playlist.Tunes.Count != 0)
        {
            foreach (var tune in playlist.Tunes)
            {
                Queue.Add(tune);
            }
        }
    }

    public void Dequeue(Tune tune)
    {
        if (tune != null && Queue.Contains(tune))
        {
            Queue.Remove(tune);
        }
    }

    public void EnqueueAll(List<Tune> tunes)
    {
        Queue.AddRange(tunes);
    }

    public void DequeueAll()
    {
        if (Queue.Count != 0)
        {
            Queue.Clear();
        }
    }

    private IMidiOutput outputDevice;
    public event EventHandler ReadyToPlayNext;
    readonly SynchronizationContext main = SynchronizationContext.Current;
    bool isStopping;

    private void PlayPlayer()
    {
        // spin wait until stopped, if pending a stop
        while (isStopping) Thread.Sleep(100);

        if (CurrentPlayer == null)
        {
            var music = GetMusic();
            if (music != null)
            {
                outputDevice = GetDevice();
                CurrentPlayer = new MidiPlayer(music, outputDevice);
                TotalTime = CurrentPlayer.GetTotalPlayTimeMilliseconds();
                CurrentTime = TimeSpan.FromMilliseconds(0);
                RemainingTime = TimeSpan.FromMilliseconds(TotalTime);
                CurrentPlayer.PlaybackCompletedToEnd += Player_Finished;
                CurrentPlayer.EventReceived += Player_EventReceived;
                CurrentPlayer.Play();
            }
            else
            {
                // found a missing/bad file; so signal move ahead
                Signal_next();
            }
        }
    }

    private void Player_Finished()
    {
        // will need this to signal next tune in the queue
        Tune.Plays += 1;
        if (Tune.Rating < 1f)    // if we got here and a rating hasn't been assigned, then default to 3
        {
            Tune.Rating = 3f;
        }
        Signal_next();
    }

    private void Signal_next()
    {
        // will need this to signal next tune in the queue
        main.Post(state => ReadyToPlayNext?.Invoke(this, new EventArgs()), null);
    }

    private void Player_EventReceived(MidiEvent m)
    {
        if (CurrentPlayer == null) return;
        if (CurrentPlayer.State != PlayerState.Playing) return;

        // be aware, if user does play next when event is firing, player may come back as null...
        // a.EventType, a.Channel, a.StatusByte, a.Value
        var prior = $"{Progress:P0}-{CurrentTime.TotalSeconds:#}";
        var time = CurrentPlayer?.PositionInTime.TotalMilliseconds ?? 0;
        var progress = time / (TotalTime + 1);
        CurrentTime = TimeSpan.FromMilliseconds(time);
        RemainingTime = TimeSpan.FromMilliseconds(TotalTime - time);

        var newp = $"{progress:P0}-{CurrentTime.TotalSeconds:#}";
        if (prior != newp) Progress = 100 * progress;  // which also signals a UI update
    }

    public void StopPlayer()
    {
        if (CurrentPlayer == null) return;
        if (isStopping)
        {
            // spin wait until stopped
            while (isStopping) Thread.Sleep(100);
        }
        else
        {
            isStopping = true;
            CurrentPlayer.Stop();
            // spin wait until stopped
            while (CurrentPlayer.State != PlayerState.Stopped) Thread.Sleep(100);
            CurrentPlayer.EventReceived -= Player_EventReceived;
            CurrentPlayer.PlaybackCompletedToEnd -= Player_Finished;
            Progress = 0;
            CurrentTime = TimeSpan.FromMilliseconds(0);
            RemainingTime = TimeSpan.FromMilliseconds(TotalTime);
            CurrentPlayer.Dispose();
            outputDevice.Dispose();
            outputDevice = null;
            CurrentPlayer = null;
            isStopping = false;
        }
    }

    public void PausePlayer()
    {
        if (CurrentPlayer == null) return;
        if (CurrentPlayer.State != PlayerState.Paused)
        {
            try
            {
                CurrentPlayer.Pause();
            }
            catch (Win32Exception) { }
        }
    }

    public void ResumePlayer()
    {
        if (CurrentPlayer == null) return;
        if (CurrentPlayer.State != PlayerState.Playing)
        {
            CurrentPlayer.Play();
        }
    }

    public void SkipPlayer(int ticks)
    {
        if (CurrentPlayer == null) return;
        SkipPlayerTo(CurrentPlayer.PlayDeltaTime + ticks);
    }

    // perform an absolute skip
    public void SkipPlayerTo(int ticks)
    {
        if (CurrentPlayer == null || ticks < 0) return;
        CurrentPlayer.Seek(ticks);
        ResumePlayer();
    }

    private MidiMusic GetMusic() => GetMusic(Path.Combine(Settings.MIDIPath, Tune.Filepath));
    private MidiMusic GetMusic(string path)
    {
        try
        {
            using var sr = File.OpenRead(path);
            return MidiMusic.Read(sr);
        }
        catch
        {
        }
        return null;
    }

    public List<IMidiPortDetails> GetDevices()
    {
        var wmm = new WinMMMidiAccess();
        return wmm.Outputs.ToList();
    }

    public IMidiOutput GetDevice()
    {
        var wmm = new WinMMMidiAccess();
        var dev = wmm.OpenOutputAsync(Settings.OutputDevice ?? "0").Result;
        return dev;
    }

    public void Dispose()
    {
        if (CurrentPlayer != null) StopPlayer();
        GC.SuppressFinalize(this);
    }

    private static string ToTitleCase(string input) => Thread.CurrentThread.CurrentCulture.TextInfo.ToTitleCase(input.ToLower());

    [GeneratedRegex(@"[\\\'\/\>\<;:\|\*?\@\=\^\!\`\~\#\u0000\u0001\u0003\u0004]", RegexOptions.IgnoreCase | RegexOptions.Compiled, "en-US")]
    private static partial Regex NotAllowed();

    [GeneratedRegex(@"[\s_-]+|\.$", RegexOptions.IgnoreCase | RegexOptions.Compiled, "en-US")]
    private static partial Regex ConvertToSpace();

    [GeneratedRegex(@"^(untitled|generated|played|written|words|\(brt\)|Copyright|http|www\.|e?mail|piano|acoustic|pedal|edition|sequenced|music\s+by|for\s+general|by\s+|words\s+|from\s+|arranged\s+|sung\s+|composed|dedicated|kmidi|melody|seq|track|this\s+and|1800S|midi\s+out|\S+\.com|\S+\.org|All Rights Reserved|with|when|just|Bdca426|dont|know|some|what|like|this|tk10|youre|Bwv001|Unnamed|comments|have|will|thing|come|v0100|midisource)", RegexOptions.IgnoreCase | RegexOptions.Compiled, "en-US")]
    private static partial Regex Ignore();
}
