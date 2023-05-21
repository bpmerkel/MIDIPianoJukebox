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
    public int Messages { get; set; }
    public int Events { get; set; }
    public int Complexity { get; set; }
    [BsonIgnore] public TimeSpan Duration => TimeSpan.FromMilliseconds(Durationms);
    public bool Equals([AllowNull] Tune x, [AllowNull] Tune y) => x?.ID == y?.ID;
    public int GetHashCode([DisallowNull] Tune obj) => obj?.ID.GetHashCode() ?? 0;
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
    public string MIDIPath { get; set; } = @"d:\MIDI";
    public string OutputDevice { get; set; } = "2";
}

public class Library : IEqualityComparer<Library>
{
    public string Name { get; set; }
    public List<Tune> Tunes { get; set; } = new List<Tune>();
    public bool Equals([AllowNull] Library x, [AllowNull] Library y) => x?.Name == y?.Name;
    public int GetHashCode([DisallowNull] Library obj) => obj?.Name.GetHashCode(StringComparison.CurrentCultureIgnoreCase) ?? 0;
}