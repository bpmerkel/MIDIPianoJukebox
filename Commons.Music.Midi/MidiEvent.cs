namespace Commons.Music.Midi;

public struct MidiEvent
{
    public const byte NoteOff = 0x80;
    public const byte NoteOn = 0x90;
    public const byte PAf = 0xA0;
    public const byte CC = 0xB0;
    public const byte Program = 0xC0;
    public const byte CAf = 0xD0;
    public const byte Pitch = 0xE0;
    public const byte SysEx1 = 0xF0;
    public const byte MtcQuarterFrame = 0xF1;
    public const byte SongPositionPointer = 0xF2;
    public const byte SongSelect = 0xF3;
    public const byte TuneRequest = 0xF6;
    public const byte SysEx2 = 0xF7;
    public const byte MidiClock = 0xF8;
    public const byte MidiTick = 0xF9;
    public const byte MidiStart = 0xFA;
    public const byte MidiContinue = 0xFB;
    public const byte MidiStop = 0xFC;
    public const byte ActiveSense = 0xFE;
    public const byte Reset = 0xFF;
    public const byte EndSysEx = 0xF7;
    public const byte Meta = 0xFF;

    public static IEnumerable<MidiEvent> Convert(byte[] bytes, int index, int size)
    {
        var i = index;
        var end = index + size;

        while (i < end)
        {
            if (bytes[i] == 0xF0)
            {
                yield return new MidiEvent(0xF0, 0, 0, bytes, index, size);
                i += size;
            }
            else
            {
                var z = FixedDataSize(bytes[i]);
                if (end < i + z)
                {
                    throw new Exception($"Received data was incomplete to build MIDI status message for '{bytes[i]:X}' status.");
                }

                yield return new MidiEvent(bytes[i],
                    (byte)(z > 0 ? bytes[i + 1] : 0),
                    (byte)(z > 1 ? bytes[i + 2] : 0),
                    null, 0, 0);
                i += z + 1;
            }
        }
    }

    public MidiEvent(int value)
    {
        Value = value;
        ExtraData = null;
        ExtraDataOffset = 0;
        ExtraDataLength = 0;
    }

    public MidiEvent(byte type, byte arg1, byte arg2, byte[] extraData, int extraDataOffset, int extraDataLength)
    {
        Value = type + (arg1 << 8) + (arg2 << 16);
        ExtraData = extraData;
        ExtraDataOffset = extraDataOffset;
        ExtraDataLength = extraDataLength;
    }

    public readonly int Value;
    // This expects EndSysEx byte _inclusive_ for F0 message.
    public readonly byte[] ExtraData;
    public readonly int ExtraDataOffset;
    public readonly int ExtraDataLength;
    public readonly byte StatusByte => (byte)(Value & 0xFF);
    public readonly byte EventType => StatusByte switch
    {
        Meta or SysEx1 or SysEx2 => StatusByte,
        _ => (byte)(Value & 0xF0),
    };
    public readonly byte Msb => (byte)((Value & 0xFF00) >> 8);
    public readonly byte Lsb => (byte)((Value & 0xFF0000) >> 16);

    public static byte FixedDataSize(byte statusByte) => (statusByte & 0xF0) switch
    {
        // and 0xF7, 0xFF
        0xF0 => statusByte switch
        {
            MtcQuarterFrame or SongSelect => 1,
            SongPositionPointer => 2,
            _ => 0,// no fixed data
        },
        Program or CAf => 1,
        _ => 2,
    };
}
