using Commons.Music.Midi;
using LiteDB;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace MIDIPianoJukebox.Data
{
    public sealed partial class JukeboxService
    {
        const string cxstring = "Filename=jukebox.db";
        private Jukebox jukebox;

        public static List<string> Log { get; } = new List<string>();

        public Task<Jukebox> GetJukeboxAsync()
        {
            return Task.Run(() =>
            {
                if (jukebox != null) return jukebox;
                lock (this)
                {
                    if (jukebox != null) return jukebox;

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

                    jukebox = new Jukebox(playlists.FindAll(), tunes.FindAll(), settings.FindOne(q => true));
                    return jukebox;
                }
            });
        }

        public static async Task AddLog(string msg) => await Task.Run(() => Log.Insert(0, $"{DateTime.Now:h:mm:ss}: {msg}")).ConfigureAwait(true);

        public Task CleanseDatabase(Action<string> logger, Action<double> progress)
        {
            return Task.Run(() =>
            {
                logger($"Cleansing database {cxstring} for MIDI files in {jukebox.Settings.MIDIPath}");
                using var db = new LiteRepository(cxstring);

                progress(.1d);

                var todelete = db.Query<Tune>()
                    .ToEnumerable()
                    .AsParallel()
                    .Where(tune => !File.Exists(Path.Combine(jukebox.Settings.MIDIPath, tune.Filepath)))
                    .ToList();

                logger($"Found {todelete.Count:#,##0} Tunes that no longer exists under {jukebox.Settings.MIDIPath}");

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

                    jukebox.Tunes = db.Query<Tune>().ToList();
                }
                progress(1d);
                logger("Database cleanse complete");
            });
        }

        public Task RefreshDatabase(Action<string> logger, Action<double> progress)
        {
            return Task.Run(() =>
            {
                logger($"Updating database {cxstring} from MIDI files in {jukebox.Settings.MIDIPath}");
                var files = new DirectoryInfo(jukebox.Settings.MIDIPath)
                    .EnumerateFiles("*.mid", SearchOption.AllDirectories)
                    .ToList();

                var total = (float)files.Count;
                logger($"Processing {total:#,##0} MIDI files");

                // use the top-level folder name as the Genre
                // pre-pend the immediate folder name in the name
                // use the file name as the name
                // capture the file path too

                var re1 = new Regex(@"[\\\'\/\>\<;:\|\*?\@\=\^\!\`\~\#\u0000\u0001\u0003\u0004]", RegexOptions.IgnoreCase | RegexOptions.Compiled);
                var re2 = new Regex(@"[\s_-]+|\.$", RegexOptions.IgnoreCase | RegexOptions.Compiled);
                var reIgnore = new Regex(@"^(untitled|generated|played|written|words|\(brt\)|Copyright|http|www\.|e?mail|piano|acoustic|pedal|edition|sequenced|music\s+by|for\s+general|by\s+|words\s+|from\s+|arranged\s+|sung\s+|composed|dedicated|kmidi|melody|seq|track|this\s+and|1800S|midi\s+out|\S+\.com|\S+\.org|All Rights Reserved|with|when|just|Bdca426|dont|know|some|what|like|this|tk10|youre|Bwv001|Unnamed|comments|have|will|thing|come|v0100|midisource)", RegexOptions.IgnoreCase | RegexOptions.Compiled);

                var counter = 0;
                var sw = Stopwatch.StartNew();

                using var db = new LiteRepository(cxstring);
                files.AsParallel().ForAll(file =>
                {
                    var subpath = file.FullName.Substring(jukebox.Settings.MIDIPath.Length + 1);
                    var library = subpath.Contains("\\") ? subpath.Substring(0, subpath.IndexOf("\\")) : subpath;
                    var name = Path.GetFileNameWithoutExtension(file.Name);
                    var tags = new List<string> { Path.GetFileName(file.DirectoryName), library };
                    var tracksCount = 0;
                    var duration = 0;

                    try
                    {
                        using var str = file.OpenRead();
                        var music = MidiMusic.Read(str);
                        duration = music.GetTotalPlayTimeMilliseconds();

                        var events = music.Tracks
                            .SelectMany(track => track.Messages)
                            .Select(msg => msg.Event)
                            .ToList();

                        tracksCount = music.Tracks.Count;

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
                    tune.Library = library;
                    tune.Filepath = subpath;
                    tune.Tracks = tracksCount;
                    tune.Durationms = duration;
                    tune.Tags = tags
                        .Where(m => !string.IsNullOrWhiteSpace(m))
                        .Where(m => m.Length > 3)
                        .Select(m => toTitleCase(m))
                        .OrderBy(m => m)
                        .Distinct()
                        .ToList();

                    if (tune.Filepath != null)
                    {
                        db.Upsert(tune);
                    }

                    Interlocked.Increment(ref counter);
                    if (counter % 500 == 0 || counter == total)
                    {
                        var rate = counter / (float)sw.ElapsedMilliseconds; // = items per ms
                        var msleft = (total - counter) / rate; // = ms
                        var eta = DateTime.Now.AddMilliseconds(msleft);
                        logger($"{(total - counter):#,##0} = {(counter / total):P0} eta: {eta:h:mm:ss} {subpath} => {library} / {name}");
                        progress(counter / total);
                    }
                });

                logger("Shrinking Database...");
                db.Database.Rebuild();
                jukebox.Tunes = db.Query<Tune>().ToList();
                logger("Database refresh complete");
            });
        }

        public void SaveSettings()
        {
            using var db = new LiteRepository(cxstring);
            db.Upsert(jukebox.Settings);
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
                var playlist = jukebox.Playlists.FirstOrDefault(p => p.Name.Equals(playlistName, StringComparison.CurrentCultureIgnoreCase));
                if (playlist != null)
                {
                    jukebox.Playlists.Remove(playlist);
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

            if (jukebox.Queue.Any())
            {
                // get the next queue item
                var idx = shuffle ? new Random().Next(0, jukebox.Queue.Count - 1) : 0;
                jukebox.Current.Tune = jukebox.Queue[idx];
                jukebox.Queue.RemoveAt(idx);
                SaveTune(jukebox.Current.Tune);

                // play 'next'
                PlayPlayer();
            }
            else
            {
                jukebox.Current.Tune = null;
            }
        }

        public void Enqueue(Tune tune)
        {
            if (tune != null && !jukebox.Queue.Contains(tune))
            {
                jukebox.Queue.Add(tune);
            }
        }

        public void Enqueue(Playlist playlist)
        {
            if (playlist != null && playlist.Tunes.Any())
            {
                foreach (var tune in playlist.Tunes)
                {
                    jukebox.Queue.Add(tune);
                }
            }
        }

        public void Dequeue(Tune tune)
        {
            if (tune != null && jukebox.Queue.Contains(tune))
            {
                jukebox.Queue.Remove(tune);
            }
        }

        public void EnqueueAll(List<Tune> tunes)
        {
            jukebox.Queue.AddRange(tunes);
        }

        public void DequeueAll()
        {
            if (jukebox.Queue.Any())
            {
                jukebox.Queue.Clear();
            }
        }

        private static readonly TextInfo textInfo = Thread.CurrentThread.CurrentCulture.TextInfo;
        private static string toTitleCase(string input) => textInfo.ToTitleCase(input.ToLower());
    }
}
