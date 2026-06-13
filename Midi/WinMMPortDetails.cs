namespace MIDIPianoJukebox.Midi;

/// <summary>
/// Windows Multimedia API implementation of MIDI port details.
/// </summary>
/// <param name="deviceId">The Windows device ID.</param>
/// <param name="name">The name of the MIDI device.</param>
/// <param name="version">The driver version.</param>
public class WinMMPortDetails(uint deviceId, string name, int version) : IMidiPortDetails
{
    public string Id { get; private set; } = deviceId.ToString();
    public string Manufacturer { get; private set; } = string.Empty;
    public string Name { get; private set; } = name;
    public string Version { get; private set; } = version.ToString();
}
