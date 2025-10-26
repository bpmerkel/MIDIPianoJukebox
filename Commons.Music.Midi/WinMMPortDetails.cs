namespace Commons.Music.Midi.WinMM;

public class WinMMPortDetails(uint deviceId, string name, int version) : IMidiPortDetails
{
    public string Id { get; private set; } = deviceId.ToString();
    public string Manufacturer { get; private set; }
    public string Name { get; private set; } = name;
    public string Version { get; private set; } = version.ToString();
}
