namespace MIDIPianoJukebox.Midi;

/// <summary>
/// Represents a MIDI port with details and connection state.
/// </summary>
public interface IMidiPort
{
    /// <summary>
    /// Gets the details of this MIDI port.
    /// </summary>
    IMidiPortDetails Details { get; }

    /// <summary>
    /// Gets the current connection state of the port.
    /// </summary>
    MidiPortConnectionState Connection { get; }

    /// <summary>
    /// Closes the MIDI port asynchronously.
    /// </summary>
    /// <returns>A task that represents the asynchronous close operation.</returns>
    Task CloseAsync();
}
