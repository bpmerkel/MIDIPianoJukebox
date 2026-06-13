namespace MIDIPianoJukebox.Midi;

/// <summary>
/// Provides details about a MIDI port.
/// </summary>
public interface IMidiPortDetails
{
    /// <summary>
    /// Gets the unique identifier of the MIDI port.
    /// </summary>
    string Id { get; }

    /// <summary>
    /// Gets the manufacturer name of the MIDI device.
    /// </summary>
    string Manufacturer { get; }

    /// <summary>
    /// Gets the name of the MIDI port.
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Gets the version of the MIDI port driver.
    /// </summary>
    string Version { get; }
}
