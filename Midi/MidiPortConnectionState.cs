namespace MIDIPianoJukebox.Midi;

/// <summary>
/// Represents the connection state of a MIDI port.
/// </summary>
public enum MidiPortConnectionState
{
    /// <summary>
    /// The port is open and ready for use.
    /// </summary>
    Open,

    /// <summary>
    /// The port is closed.
    /// </summary>
    Closed,

    /// <summary>
    /// The port connection state is pending (transitioning).
    /// </summary>
    Pending
}