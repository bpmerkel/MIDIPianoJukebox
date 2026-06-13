namespace MIDIPianoJukebox.Midi;

/// <summary>
/// Defines the result of filtering a MIDI message during seek operations.
/// </summary>
public enum SeekFilterResult
{
    /// <summary>
    /// Pass the message through and continue filtering.
    /// </summary>
    Pass,

    /// <summary>
    /// Block the message and continue filtering.
    /// </summary>
    Block,

    /// <summary>
    /// Pass the message through and terminate filtering.
    /// </summary>
    PassAndTerminate,

    /// <summary>
    /// Block the message and terminate filtering.
    /// </summary>
    BlockAndTerminate,
}