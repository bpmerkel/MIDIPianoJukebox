namespace MIDIPianoJukebox.Midi;

/// <summary>
/// Represents a MIDI output device for sending MIDI messages.
/// </summary>
public interface IMidiOutput : IMidiPort, IDisposable
{
    /// <summary>
    /// Sends a MIDI message to the output device.
    /// </summary>
    /// <param name="mevent">The MIDI event data as a byte array.</param>
    /// <param name="offset">The offset in the array where the message starts.</param>
    /// <param name="length">The length of the message.</param>
    /// <param name="timestamp">The timestamp for when the message should be sent.</param>
    void Send(byte[] mevent, int offset, int length, long timestamp);
}