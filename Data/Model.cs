namespace MIDIPianoJukebox.Data;

public class Tune : IEqualityComparer<Tune>
{
    [BsonId] public ObjectId ID { get; set; }
    public string Name { get; set; }
    public string Filepath { get; set; }
    public string Library { get; set; }
    public List<string> Tags { get; set; } = new List<string>();
    public DateTime AddedUtc { get; set; } = DateTime.UtcNow;
    public int Plays { get; set; }
    public float Rating { get; set; }
    public int Durationms { get; set; }
    public int Tracks { get; set; }
    [BsonIgnore] public TimeSpan Duration => TimeSpan.FromMilliseconds(Durationms);

    public bool Equals([AllowNull] Tune x, [AllowNull] Tune y) => x?.ID == y?.ID;
    public int GetHashCode([DisallowNull] Tune obj) => obj?.ID.GetHashCode() ?? 0;
}

public class Library : IEqualityComparer<Library>
{
    public string Name { get; set; }
    public List<Tune> Tunes { get; set; } = new List<Tune>();
    public bool Equals([AllowNull] Library x, [AllowNull] Library y) => x?.Name == y?.Name;
    public int GetHashCode([DisallowNull] Library obj) => obj?.Name.GetHashCode(StringComparison.CurrentCultureIgnoreCase) ?? 0;
}

public class Playlist : IEqualityComparer<Playlist>
{
    [BsonId] public ObjectId ID { get; set; }
    public string Name { get; set; }
    public int Plays { get; set; }
    [BsonRef(nameof(Tune))]
    public List<Tune> Tunes { get; set; } = new List<Tune>();
    public bool Equals([AllowNull] Playlist x, [AllowNull] Playlist y) => x?.ID == y?.ID;
    public int GetHashCode([DisallowNull] Playlist obj) => obj?.ID.GetHashCode() ?? 0;
}

public class Settings
{
    [BsonId] public ObjectId ID { get; set; } = ObjectId.NewObjectId();
    public string MIDIPath { get; set; } = @"e:\MIDI";
    public string OutputDevice { get; set; } = "0";
}

public enum States { Playing, Paused, Stopped }

public class Current
{
    public Tune Tune { get; set; }
    public States State { get; set; } = States.Stopped;
    public int TotalTime { get; set; }
    public TimeSpan CurrentTime { get; set; }
    public TimeSpan RemainingTime { get; set; }

    public event EventHandler ProgressChanged;

    private double _progress;
    public double Progress
    {
        get => _progress;
        set
        {
            _progress = value;
            ProgressChanged?.Invoke(this, new EventArgs());
        }
    }
}

public class Jukebox
{
    public Current Current { get; } = new Current();
    public Settings Settings { get; set; } = new Settings();
    public List<Playlist> Playlists { get; private set; }
    public List<Library> Libraries { get; private set; }
    public List<Tune> Queue { get; } = new List<Tune>();

    private List<Tune> _tunes;
    public List<Tune> Tunes
    {
        get => _tunes;
        set
        {
            _tunes = value;
            Libraries = _tunes
                .GroupBy(t => t.Library)
                .Select(g => new Library { Name = g.Key, Tunes = g.ToList() })
                .OrderBy(g => g.Name)
                .ToList();
        }
    }

    public Jukebox(IEnumerable<Playlist> playlists, IEnumerable<Tune> tunes, Settings settings)
    {
        Playlists = playlists.OrderBy(p => p.Name).ToList();
        Tunes = tunes.OrderBy(t => t.Name).ToList();
        if (settings != null) Settings = settings;
    }
}
