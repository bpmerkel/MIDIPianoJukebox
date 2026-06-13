namespace MIDIPianoJukebox.Midi;

/// <summary>
/// Represents the playback state of a MIDI player.
/// </summary>
public enum PlayerState
{
    /// <summary>
    /// The player is stopped.
    /// </summary>
    Stopped,

    /// <summary>
    /// The player is currently playing.
    /// </summary>
    Playing,

    /// <summary>
    /// The player is paused.
    /// </summary>
    Paused,
}