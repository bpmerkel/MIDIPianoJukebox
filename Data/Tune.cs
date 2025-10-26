namespace MIDIPianoJukebox.Data;

/// <summary>
/// Represents a musical tune with various properties and methods for equality comparison.
/// </summary>
public class Tune : IEqualityComparer<Tune>
{
    /// <summary>
    /// Gets or sets the unique identifier for the tune.
    /// </summary>
    [BsonId] public ObjectId ID { get; set; }

    /// <summary>
    /// Gets or sets the name of the tune.
    /// </summary>
    public string Name { get; set; }

    /// <summary>
    /// Gets or sets the file path of the tune.
    /// </summary>
    public string Filepath { get; set; }

    /// <summary>
    /// Gets or sets the tags associated with the tune.
    /// </summary>
    public string[] Tags { get; set; } = [];

    /// <summary>
    /// Gets the list of instruments used by the tune.
    /// </summary>
    public string[] Instruments { get; set; } = [];

    /// <summary>
    /// Gets or sets the date and time when the tune was added in UTC.
    /// </summary>
    public DateTime AddedUtc { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Gets or sets the number of times the tune has been played.
    /// </summary>
    public int Plays { get; set; }

    /// <summary>
    /// Gets or sets the rating of the tune.
    /// </summary>
    public float Rating { get; set; }

    /// <summary>
    /// Gets or sets the duration of the tune in milliseconds.
    /// </summary>
    public int Durationms { get; set; }

    /// <summary>
    /// Gets or sets the number of tracks in the tune.
    /// </summary>
    public int Tracks { get; set; }

    /// <summary>
    /// Gets or sets the number of messages in the tune.
    /// </summary>
    public int Messages { get; set; }

    /// <summary>
    /// Gets or sets the number of events in the tune.
    /// </summary>
    public int Events { get; set; }

    /// <summary>
    /// Gets the duration of the tune as a <see cref="TimeSpan"/>.
    /// </summary>
    [BsonIgnore] public TimeSpan Duration => TimeSpan.FromMilliseconds(Durationms);

    /// <summary>
    /// Determines whether two <see cref="Tune"/> instances are equal based on their file paths.
    /// </summary>
    /// <param name="x">The first tune to compare.</param>
    /// <param name="y">The second tune to compare.</param>
    /// <returns>
    /// <see langword="true"/> if the specified tunes have the same <see cref="Filepath"/> (case-insensitive);
    /// otherwise, <see langword="false"/>.
    /// </returns>
    public bool Equals([DisallowNull] Tune x, [DisallowNull] Tune y) => x.Filepath.Equals(y.Filepath, StringComparison.CurrentCultureIgnoreCase);

    /// <summary>
    /// Returns a hash code for the specified tune based on its file path.
    /// </summary>
    /// <param name="t">The tune for which to get a hash code.</param>
    /// <returns>
    /// A hash code computed from the tune's <see cref="Filepath"/> using a case-insensitive comparison.
    /// </returns>
    public int GetHashCode([DisallowNull] Tune t) => t.Filepath.GetHashCode(StringComparison.CurrentCultureIgnoreCase);
}