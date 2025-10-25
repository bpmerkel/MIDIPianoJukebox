namespace Commons.Music.Midi;

public interface IMidiAccess
{
    IEnumerable<IMidiPortDetails> Outputs { get; }
    Task<IMidiOutput> OpenOutputAsync(string portId);
}

#region draft API

#endregion

public interface IMidiPortDetails
{
    string Id { get; }
    string Manufacturer { get; }
    string Name { get; }
    string Version { get; }
}

public enum MidiPortConnectionState
{
    Open,
    Closed,
    Pending
}

public interface IMidiPort
{
    IMidiPortDetails Details { get; }
    MidiPortConnectionState Connection { get; }
    Task CloseAsync();
}

public interface IMidiOutput : IMidiPort, IDisposable
{
    void Send(byte[] mevent, int offset, int length, long timestamp);
}