namespace MIDIPianoJukebox.Midi;

public interface IMidiPortDetails
{
    string Id { get; }
    string Manufacturer { get; }
    string Name { get; }
    string Version { get; }
}
