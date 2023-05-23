namespace MIDIPianoJukebox.Data;

public class JukeboxService: IDisposable
{
    const string cxstring = "Filename=jukebox.db";
    public bool Loaded { get; set; } = false;
    private readonly object syncroot = new();
    public Settings Settings { get; set; } = new();
    public List<Playlist> Playlists { get; private set; }
    public List<Tune> Tunes { get; set; }
    public List<Tune> Queue { get; } = new();
    public static List<string> Log { get; } = new();
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

    public Task GetJukeboxAsync()
    {
        return Task.Run(() =>
        {
            lock (syncroot)
            {
                using var db = new LiteDatabase(cxstring);
                var tunes = db.GetCollection<Tune>();
                var playlists = db.GetCollection<Playlist>();
                var settings = db.GetCollection<Settings>();

                tunes.EnsureIndex(x => x.ID, true);
                tunes.EnsureIndex(x => x.Filepath, true);
                playlists.EnsureIndex(x => x.ID, true);
                playlists.EnsureIndex(x => x.Name, true);

                if (playlists.Count() == 0)
                {
                    playlists.Insert(new Playlist { Name = "Favorites", ID = ObjectId.NewObjectId() });
                }

                Playlists = playlists.Include(p => p.Tunes).FindAll().Where(p => p != null).OrderBy(p => p.Name).ToList();
                Tunes = tunes.FindAll().Where(t => t != null).OrderBy(t => t.Name).ToList();
                Settings = settings.FindOne(q => true);
                Loaded = true;
            }
        });
    }

    public static async Task AddLog(string msg) => await Task.Run(() => Log.Insert(0, $"{DateTime.Now:h:mm:ss}: {msg}")).ConfigureAwait(true);

    public Task CleanseDatabase(Action<string> logger, Action<double> progress)
    {
        return Task.Run(() =>
        {
            logger($"Cleansing database {cxstring} for MIDI files in {Settings.MIDIPath}");
            using var db = new LiteRepository(cxstring);

            progress(.1d);

            var todelete = db.Query<Tune>()
                .ToEnumerable()
                .AsParallel()
                .Where(tune => !File.Exists(Path.Combine(Settings.MIDIPath, tune.Filepath)))
                .ToList();

            logger($"Found {todelete.Count:#,##0} Tunes that no longer exists under {Settings.MIDIPath}");

            progress(.5d);

            if (todelete.Any())
            {
                todelete
                    .Select((tune, i) => new { tune, i })
                    .ToList()
                    .ForEach(e =>
                    {
                        db.Delete<Tune>(e.tune.ID);
                        if (e.i % 10 == 0) progress(.5 + .5d * e.i / todelete.Count);
                    });

                db.Database.Rebuild();

                Tunes = db.Query<Tune>().ToList();
            }
            progress(1d);
            logger("Database cleanse complete");
        });
    }

    public Task RefreshDatabaseAsync(Action<string> logger, Action<double> progress)
    {
        return Task.Run(() =>
        {
            logger($"Updating database {cxstring} from MIDI files in {Settings.MIDIPath}");
            var files = new DirectoryInfo(Settings.MIDIPath)
                .EnumerateFiles("*.mid", SearchOption.AllDirectories)
                .ToList();

            var total = files.Count;
            logger($"Processing {total:#,##0} MIDI files");

            // use the top-level folder name as the Genre
            // pre-pend the immediate folder name in the name
            // use the file name as the name
            // capture the file path too

            var re1 = new Regex(@"[\\\'\/\>\<;:\|\*?\@\=\^\!\`\~\#\u0000\u0001\u0003\u0004]", RegexOptions.IgnoreCase | RegexOptions.Compiled);
            var re2 = new Regex(@"[\s_-]+|\.$", RegexOptions.IgnoreCase | RegexOptions.Compiled);
            var reIgnore = new Regex(@"^(untitled|generated|played|written|words|\(brt\)|Copyright|http|www\.|e?mail|piano|acoustic|pedal|edition|sequenced|music\s+by|for\s+general|by\s+|words\s+|from\s+|arranged\s+|sung\s+|composed|dedicated|kmidi|melody|seq|track|this\s+and|1800S|midi\s+out|\S+\.com|\S+\.org|All Rights Reserved|with|when|just|Bdca426|dont|know|some|what|like|this|tk10|youre|Bwv001|Unnamed|comments|have|will|thing|come|v0100|midisource)", RegexOptions.IgnoreCase | RegexOptions.Compiled);

            var counter = 0;

            using var db = new LiteRepository(cxstring);
            files.AsParallel().ForAll(file =>
            {
                var subpath = file.FullName[(Settings.MIDIPath.Length + 1)..];
                var library = subpath.Contains('\\') ? subpath[..subpath.IndexOf('\\')] : subpath;
                var name = Path.GetFileNameWithoutExtension(file.Name);
                var tags = new List<string> { Path.GetFileName(file.DirectoryName), library };
                var tracksCount = 0;
                var messagesCount = 0;
                var eventsCount = 0;
                var complexity = 0;
                var duration = 0;

                try
                {
                    var music = GetMusic(file.FullName);
                    duration = music.GetTotalPlayTimeMilliseconds();
                    tracksCount = music.Tracks.Count;
                    var messages = music.Tracks.SelectMany(track => track.Messages).ToList();
                    messagesCount = messages.Count;

                    var events = messages.Select(msg => msg.Event).ToList();
                    eventsCount = events.Count;
                    complexity = tracksCount * eventsCount / messagesCount;

                    tags.AddRange(events
                        .Where(e => e.EventType == MidiEvent.Meta && (e.Msb == MidiMetaType.TrackName || e.Msb == MidiMetaType.Text || e.Msb == MidiMetaType.InstrumentName))
                        .Select(e => new string(Encoding.ASCII.GetChars(e.ExtraData)).Trim())
                        .Select(m => re1.Replace(m, string.Empty))
                        .Select(m => re2.Replace(m, " ").Trim())
                        .Where(m => !reIgnore.IsMatch(m))
                    );
                }
                catch (Exception ex)
                {
                    logger($"Error on {file.FullName}: {ex.Message}");
                    return;
                }

                // add/update the tune
                var tune = db.Query<Tune>()
                    .Where(t => t.Filepath.Equals(subpath, StringComparison.CurrentCultureIgnoreCase))
                    .FirstOrDefault()
                    ?? new Tune();
                tune.Name = name;
                tune.AddedUtc = DateTime.UtcNow;
                tune.Library = library;
                tune.Filepath = subpath;
                tune.Tracks = tracksCount;
                tune.Messages = messagesCount;
                tune.Events = eventsCount;
                tune.Complexity = complexity;
                tune.Durationms = duration;
                tune.Tags = tags
                    .Where(m => !string.IsNullOrWhiteSpace(m))
                    .Where(m => m.Length > 3)
                    .Select(m => ToTitleCase(m))
                    .OrderBy(m => m)
                    .Distinct()
                    .ToList();
                db.Upsert(tune);

                var sw = Stopwatch.StartNew();
                Interlocked.Increment(ref counter);
                if (counter % 500 == 0 || counter == total)
                {
                    var rate = counter / (float)sw.ElapsedMilliseconds; // = items per ms
                    var msleft = (total - counter) / rate; // = ms
                    var eta = DateTime.Now.AddMilliseconds(msleft);
                    logger($"{(total - counter):#,##0} = {(100f * counter / total):##0}% eta: {eta:h:mm:ss} {library} / {name}");
                    progress(counter / total);
                }
            });

            logger("Shrinking Database...");
            db.Database.Rebuild();
            logger("Database refresh complete");
        });
    }

    public void SaveSettings()
    {
        using var db = new LiteRepository(cxstring);
        db.Upsert(Settings);
    }

    public void SaveTune(Tune tune)
    {
        using var db = new LiteRepository(cxstring);
        db.Update(tune);
    }

    public void SavePlaylist(Playlist playlist)
    {
        if (playlist is null)
        {
            throw new ArgumentNullException(nameof(playlist));
        }

        using var db = new LiteRepository(cxstring);
        if (playlist.Tunes.Count > 0)
        {
            db.Upsert(playlist);
        }
        else
        {
            db.Delete<Playlist>(playlist.ID);
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
                using var db = new LiteRepository(cxstring);
                db.Delete<Playlist>(playlist.ID);
            }
        }
    }

    // get the next Tune from the queue and play it
    public void PlayNext(bool shuffle)
    {
        // interrupt any current playing Tune
        StopPlayer();

        if (Queue.Any())
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
        if (playlist != null && playlist.Tunes.Any())
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
        if (Queue.Any())
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
        if (prior != newp) Progress = progress;  // which also signals a UI update
    }

    private void StopPlayer()
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
            return null;
        }
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
}
