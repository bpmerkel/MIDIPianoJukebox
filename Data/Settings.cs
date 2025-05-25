namespace MIDIPianoJukebox.Data;

/// <summary>
/// Represents the settings for the MIDI piano jukebox application.
/// </summary>
public class Settings
{
    /// <summary>
    /// Gets or sets the unique identifier for the settings.
    /// </summary>
    [BsonId] public ObjectId ID { get; set; } = ObjectId.NewObjectId();

    /// <summary>
    /// Gets or sets the path to the MIDI files.
    /// </summary>
    public string MIDIPath { get; set; } = @"C:\Users\brady\Downloads\MIDI";

    /// <summary>
    /// Gets or sets the output device identifier.
    /// </summary>
    public string OutputDevice { get; set; } = "2";
}
