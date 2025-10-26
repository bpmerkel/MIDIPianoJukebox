namespace Commons.Music.Midi;

public interface IMidiAccess
{
    IEnumerable<IMidiPortDetails> Outputs { get; }
    Task<IMidiOutput> OpenOutputAsync(string portId);
}
