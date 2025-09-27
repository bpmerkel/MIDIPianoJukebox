namespace Commons.Music.Midi;

public partial class MidiAccessManager
{
    static MidiAccessManager()
    {
        Default = Empty = new EmptyMidiAccess();
        new MidiAccessManager().InitializeDefault();
    }

    private MidiAccessManager()
    {
        // We need this only for that we want to use partial method!
    }

    public static IMidiAccess Default { get; private set; }
    public static IMidiAccess Empty { get; internal set; }
    partial void InitializeDefault();
}

public interface IMidiAccess
{
    IEnumerable<IMidiPortDetails> Inputs { get; }
    IEnumerable<IMidiPortDetails> Outputs { get; }
    Task<IMidiInput> OpenInputAsync(string portId);
    Task<IMidiOutput> OpenOutputAsync(string portId);
    MidiAccessExtensionManager ExtensionManager { get; }
}

#region draft API

public class MidiAccessExtensionManager
{
    public virtual bool Supports<T>() where T : class => GetInstance<T>() != default(T);
    public virtual T GetInstance<T>() where T : class => null;
}

public abstract class MidiPortCreatorExtension
{
    public abstract IMidiOutput CreateVirtualInputSender(PortCreatorContext context);
    public abstract IMidiInput CreateVirtualOutputReceiver(PortCreatorContext context);
    public delegate void SendDelegate(byte[] buffer, int index, int length, long timestamp);
    public class PortCreatorContext
    {
        public string ApplicationName { get; set; }
        public string PortName { get; set; }
        public string Manufacturer { get; set; }
        public string Version { get; set; }
    }
}

public abstract class SimpleVirtualMidiPort(IMidiPortDetails details, Action onDispose) : IMidiPort
{
    MidiPortConnectionState connection = MidiPortConnectionState.Open;
    public IMidiPortDetails Details => details;
    public MidiPortConnectionState Connection => connection;
    public Task CloseAsync() => Task.Run(() =>
    {
        onDispose?.Invoke();
        connection = MidiPortConnectionState.Closed;
    });
    public void Dispose() => CloseAsync().Wait();
}

public class SimpleVirtualMidiOutput(IMidiPortDetails details, Action onDispose) : SimpleVirtualMidiPort(details, onDispose), IMidiOutput
{
    public MidiPortCreatorExtension.SendDelegate OnSend { get; set; }
    public void Send(byte[] mevent, int offset, int length, long timestamp) => OnSend?.Invoke(mevent, offset, length, timestamp);
}

#endregion

public class MidiConnectionEventArgs : EventArgs
{
    public IMidiPortDetails Port { get; private set; }
}

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

public interface IMidiInput : IMidiPort, IDisposable
{
}

public interface IMidiOutput : IMidiPort, IDisposable
{
    void Send(byte[] mevent, int offset, int length, long timestamp);
}

public class MidiReceivedEventArgs : EventArgs
{
    public long Timestamp { get; set; }
    public byte[] Data { get; set; }
    public int Start { get; set; }
    public int Length { get; set; }
}

class EmptyMidiAccess : IMidiAccess
{
    public IEnumerable<IMidiPortDetails> Inputs
    {
        get { yield return EmptyMidiInput.Instance.Details; }
    }

    public IEnumerable<IMidiPortDetails> Outputs
    {
        get { yield return EmptyMidiOutput.Instance.Details; }
    }

    public MidiAccessExtensionManager ExtensionManager => throw new NotImplementedException();

    public Task<IMidiInput> OpenInputAsync(string portId)
    {
        if (portId != EmptyMidiInput.Instance.Details.Id)
        {
            throw new ArgumentException($"Port ID {portId} does not exist.");
        }

        return Task.FromResult<IMidiInput>(EmptyMidiInput.Instance);
    }

    public Task<IMidiOutput> OpenOutputAsync(string portId)
    {
        if (portId != EmptyMidiOutput.Instance.Details.Id)
        {
            throw new ArgumentException($"Port ID {portId} does not exist.");
        }

        return Task.FromResult<IMidiOutput>(EmptyMidiOutput.Instance);
    }
}

abstract class EmptyMidiPort : IMidiPort
{
    readonly Task completed_task = Task.FromResult(false);
    public IMidiPortDetails Details => CreateDetails();
    internal abstract IMidiPortDetails CreateDetails();
    public MidiPortConnectionState Connection { get; private set; }
    // do nothing.
    public Task CloseAsync() => completed_task;

    public void Dispose()
    {
    }
}

class EmptyMidiPortDetails(string id, string name) : IMidiPortDetails
{
    public string Id { get; set; } = id;
    public string Manufacturer { get; set; } = "dummy project";
    public string Name { get; set; } = name;
    public string Version { get; set; } = "0.0";
}

class EmptyMidiInput : EmptyMidiPort, IMidiInput
{
    static EmptyMidiInput()
    {
        Instance = new EmptyMidiInput();
    }

    public static EmptyMidiInput Instance { get; private set; }

    internal override IMidiPortDetails CreateDetails() => new EmptyMidiPortDetails("dummy_in", "Dummy MIDI Input");
}

class EmptyMidiOutput : EmptyMidiPort, IMidiOutput
{
    readonly Task completed_task = Task.FromResult(false);

    static EmptyMidiOutput()
    {
        Instance = new EmptyMidiOutput();
    }

    public static EmptyMidiOutput Instance { get; private set; }

    public void Send(byte[] mevent, int offset, int length, long timestamp)
    {
        // do nothing.
    }

    internal override IMidiPortDetails CreateDetails() => new EmptyMidiPortDetails("dummy_out", "Dummy MIDI Output");
}
