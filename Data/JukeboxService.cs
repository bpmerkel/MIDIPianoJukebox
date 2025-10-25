namespace MIDIPianoJukebox.Data;

/// <summary>
/// The JukeboxService class is responsible for managing the jukebox's state and behavior.
/// It provides methods for loading the jukebox, adding logs, refreshing the database, saving settings, tunes, and playlists,
/// managing the play queue, controlling the player, and getting device information.
/// </summary>
public partial class JukeboxService : IDisposable
{
    /// <summary>
    /// Connection string for the database.
    /// </summary>
    private const string cxstring = "Filename=jukebox.db;Mode=shared;Upgrade=true";

    /// <summary>
    /// Repository for accessing the database.
    /// </summary>
    private readonly LiteRepository repo = new(cxstring);

    /// <summary>
    /// Object for thread synchronization.
    /// </summary>
    private readonly Lock syncroot = new();

    /// <summary>
    /// Gets a value indicating whether the jukebox is loaded.
    /// </summary>
    public bool Loaded { get; private set; } = false;

    /// <summary>
    /// Gets the settings of the jukebox.
    /// </summary>
    public Settings Settings { get; private set; } = new();

    /// <summary>
    /// Gets the playlists of the jukebox.
    /// </summary>
    public List<Playlist> Playlists { get; private set; }

    /// <summary>
    /// Gets or sets the tunes of the jukebox.
    /// </summary>
    public List<Tune> Tunes { get; set; }

    /// <summary>
    /// Gets the queue of the jukebox.
    /// </summary>
    public List<Tune> Queue { get; } = [];

    /// <summary>
    /// Gets the log of the jukebox.
    /// </summary>
    public static ConcurrentStack<string> Log { get; } = new();

    /// <summary>
    /// Gets the current tune of the jukebox.
    /// </summary>
    public Tune Tune { get; private set; }

    /// <summary>
    /// Gets the total time of the current tune.
    /// </summary>
    public int TotalTime { get; private set; }

    /// <summary>
    /// Gets the current time of the current tune.
    /// </summary>
    public TimeSpan CurrentTime { get; private set; }

    /// <summary>
    /// Gets the remaining time of the current tune.
    /// </summary>
    public TimeSpan RemainingTime { get; private set; }

    /// <summary>
    /// Gets the current player of the jukebox.
    /// </summary>
    public MidiPlayer CurrentPlayer { get; private set; }

    /// <summary>
    /// Gets the state of the current player.
    /// </summary>
    public PlayerState State => CurrentPlayer?.State ?? PlayerState.Stopped;

    /// <summary>
    /// Gets or sets the action to be performed when the progress changes.
    /// </summary>
    public Action<double> ProgressChanged { get; set; }

    /// <summary>
    /// Progress of the current operation.
    /// </summary>
    private double _progress = 0d;

    /// <summary>
    /// Gets or sets the progress of the current tune.
    /// </summary>
    public double Progress
    {
        get => _progress;
        private set
        {
            _progress = value;
            ProgressChanged(value);
        }
    }

    /// <summary>
    /// Asynchronously loads the jukebox data from the database.
    /// </summary>
    public async Task GetJukeboxAsync()
    {
        if (Loaded)
        {
            return;
        }

        await Task.Run(() =>
        {
            if (Loaded)
            {
                return;
            }

            lock (syncroot)
            {
                if (Loaded)
                {
                    return;
                }

                var tunes = repo.Database.GetCollection<Tune>();
                var playlists = repo.Database.GetCollection<Playlist>();
                var settings = repo.Database.GetCollection<Settings>();

                tunes.EnsureIndex(x => x.ID, true);
                tunes.EnsureIndex(x => x.Filepath, true);
                playlists.EnsureIndex(x => x.ID, true);
                playlists.EnsureIndex(x => x.Name, true);

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

                // remove tunes from playlists that don't meet the filter rules
                // use a Tune for equality comparison
                Playlists.ForEach(p => p.Tunes = p.Tunes.Intersect(Tunes, new Tune()).ToList());
                Console.WriteLine($"Loaded {Tunes.Count} tunes, {Playlists.Count} playlists");
                Loaded = true;
            }
        });
    }

    /// <summary>
    /// Asynchronously adds a log message to the jukebox log.
    /// </summary>
    /// <param name="msg">The message to add to the log.</param>
    public static async Task AddLog(string msg) => await Task.Run(() => Log.Push($"{DateTime.Now:h:mm:ss}: {msg}")).ConfigureAwait(true);

    /// <summary>
    /// Asynchronously refreshes the jukebox database.
    /// </summary>
    /// <param name="doUpdates">Whether to perform updates.</param>
    /// <param name="logger">The logger action.</param>
    /// <param name="progress">The progress action.</param>
    public Task RefreshDatabaseAsync(Action<string> logger, Action<double> progress)
    {
        return Task.Run(async () =>
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
            var sw = Stopwatch.StartNew();

            var toRemove = repo.Database.GetCollection<Tune>().FindAll()
                .Where(t => !File.Exists(Path.Combine(Settings.MIDIPath, t.Filepath)))
                .ToList();
            logger($"Removing {toRemove.Count:#,##0} missing tunes");
            toRemove.ForEach(t => repo.Delete<Tune>(t.ID));

            files.ForEach(file =>
            {
                try
                {
                    var subpath = file.FullName[(Settings.MIDIPath.Length + 1)..];

                    Interlocked.Increment(ref counter);
                    if (counter % 500 == 0 || counter == total)
                    {
                        var rate = counter / (float)sw.ElapsedMilliseconds; // = items per ms
                        var msleft = (total - counter) / rate; // = ms
                        var eta = DateTime.Now.AddMilliseconds(msleft);
                        logger($"{(total - counter):#,##0} = {(100f * counter / total):##0}% eta: {eta:h:mm:ss} {subpath}");
                        progress(100 * counter / total);
                    }

                    var music = GetMusic(file.FullName);

                    if (music == null)
                    {
                        return;
                    }

                    var duration = music.GetTotalPlayTimeMilliseconds();

                    if (duration < 60_000)
                    {
                        return;
                    }

                    var name = Path.GetFileNameWithoutExtension(file.Name).Replace('_', ' ');
                    var tracksCount = music.Tracks.Count;
                    var messages = music.Tracks.SelectMany(track => track.Messages).ToArray();
                    var messagesCount = messages.Length;
                    var events = messages.Select(msg => msg.Event).ToArray();
                    var eventsCount = events.Length;
                    var tags = events
                        .Where(e => e.EventType == MidiEvent.Meta && (e.Msb == MidiMetaType.TrackName || e.Msb == MidiMetaType.Text || e.Msb == MidiMetaType.InstrumentName))
                        .Select(e => new string(Encoding.ASCII.GetChars(e.ExtraData)).Trim())
                        .Select(m => re1.Replace(m, string.Empty))
                        .Select(m => re2.Replace(m, " ").Trim())
                        .Where(m => !reIgnore.IsMatch(m))
                        .Where(m => !string.IsNullOrWhiteSpace(m))
                        .Where(m => m.Length > 3)
                        .Select(m => ToTitleCase(m))
                        .OrderBy(m => m)
                        .Distinct()
                        .ToArray();

                    // add/update the tune
                    var tune = repo.Query<Tune>()
                        .Where(t => t.Filepath.Equals(subpath, StringComparison.CurrentCultureIgnoreCase))
                        .FirstOrDefault();

                    if (tune != null)
                    {
                        tune.Name = name;
                        tune.Filepath = subpath;
                        tune.Tracks = tracksCount;
                        tune.Messages = messagesCount;
                        tune.Events = eventsCount;
                        tune.Durationms = duration;
                        tune.Tags = tags;
                        // tune.Rating // leave alone
                        // tune.AddedUtc // leave alone
                        repo.Update(tune);
                    }
                    else
                    {
                        repo.Insert(new Tune
                        {
                            Name = name,
                            Filepath = subpath,
                            Tracks = tracksCount,
                            Messages = messagesCount,
                            Events = eventsCount,
                            Durationms = duration,
                            Tags = tags,
                            Rating = 0f,
                            AddedUtc = DateTime.UtcNow
                        });
                    }
                }
                catch (Exception ex)
                {
                    logger($"Error on {file.FullName}: {ex.Message}");
                    return;
                }
            });

            //logger("Shrinking Database...");
            //repo.Database.Rebuild();
            //logger("Database refresh complete");

            Loaded = false;
            await GetJukeboxAsync();
        });
    }

    /// <summary>
    /// Saves the jukebox settings to the database.
    /// </summary>
    public void SaveSettings() => repo.Upsert(Settings);

    /// <summary>
    /// Saves a tune to the database.
    /// </summary>
    /// <param name="tune">The tune to save.</param>
    public void SaveTune(Tune tune) => repo.Update(tune);

    /// <summary>
    /// Saves a playlist to the database.
    /// </summary>
    /// <param name="playlist">The playlist to save.</param>
    public void SavePlaylist(Playlist playlist)
    {
        ArgumentNullException.ThrowIfNull(playlist);

        if (playlist.Tunes.Count > 0)
        {
            repo.Upsert(playlist);
            Playlists.Add(playlist);
        }
        else
        {
            repo.Delete<Playlist>(playlist.ID);
        }
    }

    /// <summary>
    /// Clears a playlist from the jukebox.
    /// </summary>
    /// <param name="playlistName">The name of the playlist to clear.</param>
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

    /// <summary>
    /// Get the next Tune from the queue and play it.
    /// </summary>
    /// <param name="shuffle">Whether to shuffle the queue.</param>
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

    /// <summary>
    /// Set and play a specific Tune from the queue.
    /// </summary>
    /// <param name="tune">The tune to play.</param>
    public void Play(Tune tune)
    {
        var found = Queue.FirstOrDefault(t => t.ID == tune.ID);

        if (found != null)
        {
            // interrupt any current playing Tune
            StopPlayer();
            Queue.Remove(found);
            Tune = tune;
            PlayPlayer();
        }
    }

    /// <summary>
    /// Enqueues a tune to the jukebox queue.
    /// </summary>
    /// <param name="tune">The tune to enqueue.</param>
    public void Enqueue(Tune tune)
    {
        if (tune != null && !Queue.Contains(tune))
        {
            Queue.Add(tune);
        }
    }

    /// <summary>
    /// Enqueues a playlist to the jukebox queue.
    /// </summary>
    /// <param name="playlist">The playlist to enqueue.</param>
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

    /// <summary>
    /// Dequeues a tune from the jukebox queue.
    /// </summary>
    /// <param name="tune">The tune to dequeue.</param>
    public void Dequeue(Tune tune)
    {
        if (tune != null && Queue.Contains(tune))
        {
            Queue.Remove(tune);
        }
    }

    /// <summary>
    /// Enqueues a list of tunes to the jukebox queue.
    /// </summary>
    /// <param name="tunes">The list of tunes to enqueue.</param>
    public void EnqueueAll(List<Tune> tunes) => Queue.AddRange(tunes);

    /// <summary>
    /// Dequeues all tunes from the jukebox queue.
    /// </summary>
    public void DequeueAll()
    {
        if (Queue.Count != 0)
        {
            Queue.Clear();
        }
    }

    /// <summary>
    /// The MIDI output device.
    /// </summary>
    private IMidiOutput outputDevice;

    /// <summary>
    /// Event that is triggered when the jukebox is ready to play the next tune.
    /// </summary>
    public event EventHandler ReadyToPlayNext;

    /// <summary>
    /// The main synchronization context.
    /// </summary>
    private readonly SynchronizationContext main = SynchronizationContext.Current;

    /// <summary>
    /// Flag indicating if the service is stopping.
    /// </summary>
    private bool isStopping;

    /// <summary>
    /// Plays the current tune in the jukebox.
    /// </summary>
    private void PlayPlayer()
    {
        // spin wait until stopped, if pending a stop
        while (isStopping)
        {
            Thread.Sleep(100);
        }

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

    /// <summary>
    /// Handles the event when the player finishes playing a tune.
    /// </summary>
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

    /// <summary>
    /// Signals the jukebox to play the next tune in the queue.
    /// </summary>
    private void Signal_next()
    {
        // will need this to signal next tune in the queue
        main.Post(state => ReadyToPlayNext?.Invoke(this, new EventArgs()), null);
    }

    /// <summary>
    /// Handles the event when the player receives a MIDI event.
    /// </summary>
    /// <param name="m">The MIDI event received.</param>
    private void Player_EventReceived(MidiEvent m)
    {
        if (CurrentPlayer == null)
        {
            return;
        }

        if (CurrentPlayer.State != PlayerState.Playing)
        {
            return;
        }

        // be aware, if user does play next when event is firing, player may come back as null...
        // a.EventType, a.Channel, a.StatusByte, a.Value
        var prior = $"{Progress:P0}-{CurrentTime.TotalSeconds:#}";
        var time = CurrentPlayer?.PositionInTime.TotalMilliseconds ?? 0;
        var progress = time / (TotalTime + 1);
        CurrentTime = TimeSpan.FromMilliseconds(time);
        RemainingTime = TimeSpan.FromMilliseconds(TotalTime - time);

        var newp = $"{progress:P0}-{CurrentTime.TotalSeconds:#}";

        if (prior != newp)
        {
            Progress = 100 * progress;  // which also signals a UI update
        }
    }

    /// <summary>
    /// Stops the current tune in the jukebox.
    /// </summary>
    public void StopPlayer()
    {
        if (CurrentPlayer == null)
        {
            return;
        }

        if (isStopping)
        {
            // spin wait until stopped
            while (isStopping)
            {
                Thread.Sleep(100);
            }
        }
        else
        {
            isStopping = true;
            CurrentPlayer.Stop();

            // spin wait until stopped
            while (CurrentPlayer.State != PlayerState.Stopped)
            {
                Thread.Sleep(100);
            }

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

    /// <summary>
    /// Pauses the current tune in the jukebox.
    /// </summary>
    public void PausePlayer()
    {
        if (CurrentPlayer == null)
        {
            return;
        }

        if (CurrentPlayer.State != PlayerState.Paused)
        {
            try
            {
                CurrentPlayer.Pause();
            }
            catch (Win32Exception) { }
        }
    }

    /// <summary>
    /// Resumes the current tune in the jukebox.
    /// </summary>
    public void ResumePlayer()
    {
        if (CurrentPlayer == null)
        {
            return;
        }

        if (CurrentPlayer.State != PlayerState.Playing)
        {
            CurrentPlayer.Play();
        }
    }

    /// <summary>
    /// Skips a certain number of ticks in the current tune.
    /// </summary>
    /// <param name="ticks">The number of ticks to skip.</param>
    public void SkipPlayer(int ticks)
    {
        if (CurrentPlayer == null)
        {
            return;
        }

        SkipPlayerTo(CurrentPlayer.PlayDeltaTime + ticks);
    }

    /// <summary>
    /// Skips to a certain tick in the current tune.
    /// </summary>
    /// <param name="ticks">The tick to skip to.</param>
    public void SkipPlayerTo(int ticks)
    {
        if (CurrentPlayer == null || ticks < 0)
        {
            return;
        }

        CurrentPlayer.Seek(ticks);
        ResumePlayer();
    }

    /// <summary>
    /// Gets the music for the current tune.
    /// </summary>
    /// <returns>The music for the current tune.</returns>
    private MidiMusic GetMusic() => GetMusic(Path.Combine(Settings.MIDIPath, Tune.Filepath));

    /// <summary>
    /// Gets the music from a MIDI file.
    /// </summary>
    /// <param name="path">The path to the MIDI file.</param>
    /// <returns>The music from the MIDI file.</returns>
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

    /// <summary>
    /// Gets a list of MIDI devices.
    /// </summary>
    /// <returns>A list of MIDI devices.</returns>
    public List<IMidiPortDetails> GetDevices()
    {
        var wmm = new WinMMMidiAccess();
        return wmm.Outputs.ToList();
    }

    /// <summary>
    /// Gets a MIDI output device.
    /// </summary>
    /// <returns>A MIDI output device.</returns>
    public IMidiOutput GetDevice()
    {
        var wmm = new WinMMMidiAccess();
        var dev = wmm.OpenOutputAsync(Settings.OutputDevice ?? "0").Result;
        return dev;
    }

    /// <summary>
    /// Disposes of the jukebox service.
    /// </summary>
    public void Dispose()
    {
        if (CurrentPlayer != null)
        {
            StopPlayer();
        }

        repo.Dispose();
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Converts a string to title case.
    /// </summary>
    /// <param name="input">The string to convert.</param>
    /// <returns>The string in title case.</returns>
    private static string ToTitleCase(string input) => Thread.CurrentThread.CurrentCulture.TextInfo.ToTitleCase(input.ToLower());

    /// <summary>
    /// Gets a regex for matching non-alphabetic characters at the start of a string.
    /// </summary>
    /// <returns>The regex for matching non-alphabetic characters at the start of a string.</returns>
    [GeneratedRegex(@"^[^a-zA-Z]+", RegexOptions.IgnoreCase | RegexOptions.Compiled, "en-US")]
    private static partial Regex NotAllowed();

    /// <summary>
    /// Gets a regex for matching one or more spaces, underscores, or hyphens, or one or more periods at the end of a string.
    /// </summary>
    /// <returns>The regex for matching one or more spaces, underscores, or hyphens, or one or more periods at the end of a string.</returns>
    [GeneratedRegex(@"[\s_-]+|\.+$", RegexOptions.IgnoreCase | RegexOptions.Compiled, "en-US")]
    private static partial Regex ConvertToSpace();

    /// <summary>
    /// Gets a regex for matching certain words and phrases that should be ignored.
    /// </summary>
    /// <returns>The regex for matching certain words and phrases that should be ignored.</returns>
    [GeneratedRegex(@"^(untitled|generated|played|written|words|\(brt\)|Copyright|http|www\.|e?mail|piano|acoustic|pedal|edition|sequenced|music\s+by|for\s+general|by\s+|words\s+|from\s+|arranged\s+|sung\s+|composed|dedicated|kmidi|melody|seq|track|this\s+and|1800S|midi\s+out|\S+\.com|\S+\.org|All Rights Reserved|with|when|just|Bdca426|dont|know|some|what|like|this|tk10|youre|Bwv001|Unnamed|comments|have|will|thing|come|v0100|midisource)", RegexOptions.IgnoreCase | RegexOptions.Compiled, "en-US")]
    private static partial Regex Ignore();
}