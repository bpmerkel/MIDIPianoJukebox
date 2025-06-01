namespace MIDIPianoJukebox.Data;

/// <summary>
/// Represents a playlist containing a collection of tunes.
/// </summary>
public class Playlist
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
}