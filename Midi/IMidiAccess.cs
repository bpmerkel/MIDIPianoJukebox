namespace MIDIPianoJukebox.Midi;

/// <summary>
/// Provides access to MIDI output devices.
/// </summary>
public interface IMidiAccess
{
    /// <summary>
    /// Gets the available MIDI output devices.
    /// </summary>
    IEnumerable<IMidiPortDetails> Outputs { get; }

    /// <summary>
    /// Opens a MIDI output device for sending messages.
    /// </summary>
    /// <param name="portId">The unique identifier of the MIDI output port.</param>
    /// <returns>A task that represents the asynchronous operation and returns an IMidiOutput instance.</returns>
    Task<IMidiOutput> OpenOutputAsync(string portId);
}
