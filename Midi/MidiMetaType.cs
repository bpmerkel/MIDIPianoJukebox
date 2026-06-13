namespace MIDIPianoJukebox.Midi;

/// <summary>
/// Defines MIDI meta event types and utility methods.
/// </summary>
public static class MidiMetaType
{
    public const byte SequenceNumber = 0x00;
    public const byte Text = 0x01;
    public const byte Copyright = 0x02;
    public const byte TrackName = 0x03;
    public const byte InstrumentName = 0x04;
    public const byte Lyric = 0x05;
    public const byte Marker = 0x06;
    public const byte Cue = 0x07;
    public const byte ChannelPrefix = 0x20;
    public const byte EndOfTrack = 0x2F;
    public const byte Tempo = 0x51;
    public const byte SmpteOffset = 0x54;
    public const byte TimeSignature = 0x58;
    public const byte KeySignature = 0x59;
    public const byte SequencerSpecific = 0x7F;
    public const int DefaultTempo = 500000;

    /// <summary>
    /// Extracts the tempo value from MIDI meta event data.
    /// </summary>
    /// <param name="data">The byte array containing tempo data.</param>
    /// <param name="offset">The offset in the array where tempo data starts.</param>
    /// <returns>The tempo value in microseconds per quarter note.</returns>
    public static int GetTempo(byte[] data, int offset)
    {
        ArgumentNullException.ThrowIfNull(data);

        if (offset < 0 || offset + 2 >= data.Length)
        {
            throw new ArgumentException($"offset + 2 must be a valid size under data length of array size {data.Length}; {offset} is not.");
        }

        return (data[offset] << 16) + (data[offset + 1] << 8) + data[offset + 2];
    }
}