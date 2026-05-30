namespace MIDIPianoJukebox.Midi;

public interface IMidiOutput : IMidiPort, IDisposable
{
    void Send(byte[] mevent, int offset, int length, long timestamp);
}