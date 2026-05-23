namespace MIDIPianoJukebox.Data;

/// <summary>
/// Represents a playlist containing a collection of tunes.
/// </summary>
public class Playlist : IEqualityComparer<Playlist>
{
    /// <summary>
    /// Gets or sets the unique identifier for the playlist.
    /// </summary>
    [BsonId] public ObjectId ID { get; set; }

    /// <summary>
    /// Gets or sets the name of the playlist.
    /// </summary>
    public string Name { get; set; }

    /// <summary>
    /// Gets or sets the list of tunes in the playlist.
    /// </summary>
    [BsonRef(nameof(Tune))]
    public List<Tune> Tunes { get; set; } = [];

    /// <summary>
    /// Determines whether two <see cref="Playlist"/> instances are equal based on their names.
    /// </summary>
    /// <param name="x">The first playlist to compare.</param>
    /// <param name="y">The second playlist to compare.</param>
    /// <returns>
    /// <see langword="true"/> if the specified playlists have the same <see cref="Name"/> (case-insensitive);
    /// otherwise, <see langword="false"/>.
    /// </returns>
    public bool Equals([DisallowNull] Playlist x, [DisallowNull] Playlist y) => x.Name.Equals(y.Name, StringComparison.CurrentCultureIgnoreCase);

    /// <summary>
    /// Returns a hash code for the specified playlist based on its name.
    /// </summary>
    /// <param name="p">The playlist for which to get a hash code.</param>
    /// <returns>
    /// A hash code computed from the playlist's <see cref="Name"/> using a case-insensitive comparison.
    /// </returns>
    public int GetHashCode([DisallowNull] Playlist p) => p.Name.GetHashCode(StringComparison.CurrentCultureIgnoreCase);
}