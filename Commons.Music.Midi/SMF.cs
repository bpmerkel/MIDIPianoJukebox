namespace Commons.Music.Midi;

public class MidiMusic
{
    #region static members

    public static MidiMusic Read(Stream stream)
    {
        var r = new SmfReader();
        r.Read(stream);
        return r.Music;
    }

    #endregion

    private readonly List<MidiTrack> tracks = [];

    public MidiMusic()
    {
        Format = 1;
    }

    public short DeltaTimeSpec { get; set; }

    public byte Format { get; set; }

    public IList<MidiTrack> Tracks => tracks;

    public IEnumerable<MidiMessage> GetMetaEventsOfType(byte metaType)
    {
        if (Format != 0)
        {
            return SmfTrackMerger.Merge(this).GetMetaEventsOfType(metaType);
        }

        return GetMetaEventsOfType(tracks[0].Messages, metaType);
    }

    public static IEnumerable<MidiMessage> GetMetaEventsOfType(IEnumerable<MidiMessage> messages, byte metaType)
    {
        var v = 0;
        foreach (var m in messages)
        {
            v += m.DeltaTime;
            if (m.Event.EventType == MidiEvent.Meta && m.Event.Msb == metaType)
            {
                yield return new MidiMessage(v, m.Event);
            }
        }
    }

    public int GetTotalTicks()
    {
        if (Format != 0)
        {
            return SmfTrackMerger.Merge(this).GetTotalTicks();
        }

        return Tracks[0].Messages.Sum(m => m.DeltaTime);
    }

    public int GetTotalPlayTimeMilliseconds()
    {
        if (Format != 0)
        {
            return SmfTrackMerger.Merge(this).GetTotalPlayTimeMilliseconds();
        }

        return GetTotalPlayTimeMilliseconds(Tracks[0].Messages, DeltaTimeSpec);
    }

    public int GetTimePositionInMillisecondsForTick(int ticks)
    {
        if (Format != 0)
        {
            return SmfTrackMerger.Merge(this).GetTimePositionInMillisecondsForTick(ticks);
        }

        return GetPlayTimeMillisecondsAtTick(Tracks[0].Messages, ticks, DeltaTimeSpec);
    }

    public static int GetTotalPlayTimeMilliseconds(IList<MidiMessage> messages, int deltaTimeSpec) => GetPlayTimeMillisecondsAtTick(messages, messages.Sum(m => m.DeltaTime), deltaTimeSpec);

    public static int GetPlayTimeMillisecondsAtTick(IList<MidiMessage> messages, int ticks, int deltaTimeSpec)
    {
        if (deltaTimeSpec < 0)
        {
            throw new NotSupportedException("non-tick based DeltaTime");
        }

        var tempo = MidiMetaType.DefaultTempo;
        var t = 0;
        var v = 0d;

        foreach (var m in messages)
        {
            var deltaTime = t + m.DeltaTime < ticks ? m.DeltaTime : ticks - t;
            v += (double)tempo / 1000 * deltaTime / deltaTimeSpec;
            if (deltaTime != m.DeltaTime)
            {
                break;
            }

            t += m.DeltaTime;
            if (m.Event.EventType == MidiEvent.Meta && m.Event.Msb == MidiMetaType.Tempo)
            {
                tempo = MidiMetaType.GetTempo(m.Event.ExtraData, m.Event.ExtraDataOffset);
            }
        }
        return (int)v;
    }
}

public class MidiTrack
{
    public MidiTrack()
        : this([])
    {
    }

    public MidiTrack(IList<MidiMessage> messages)
    {
        ArgumentNullException.ThrowIfNull(messages);
        this.messages = messages as List<MidiMessage> ?? [.. messages];
    }

    private readonly List<MidiMessage> messages;

    public IList<MidiMessage> Messages => messages;
}

public readonly struct MidiMessage(int deltaTime, MidiEvent evt)
{
    public readonly int DeltaTime = deltaTime;
    public readonly MidiEvent Event = evt;
    public override string ToString() => $"[dt{DeltaTime}]{Event}";
}

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

    public static byte FixedDataSize(byte statusByte)
    {
        return (statusByte & 0xF0) switch
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
}

public class SmfReader
{
    private Stream stream;
    private MidiMusic data;
    public MidiMusic Music => data;

    public void Read(Stream stream)
    {
        ArgumentNullException.ThrowIfNull(stream);

        this.stream = stream;
        data = new MidiMusic();
        try
        {
            DoParse();
        }
        finally
        {
            this.stream = null;
        }
    }

    private void DoParse()
    {
        if (ReadByte() != 'M'
            || ReadByte() != 'T'
            || ReadByte() != 'h'
            || ReadByte() != 'd')
        {
            throw ParseError("MThd is expected");
        }

        if (ReadInt32() != 6)
        {
            throw ParseError("Unexpected data size (should be 6)");
        }

        data.Format = (byte)ReadInt16();
        var tracks = ReadInt16();
        data.DeltaTimeSpec = ReadInt16();
        try
        {
            for (var i = 0; i < tracks; i++)
            {
                data.Tracks.Add(ReadTrack());
            }
        }
        catch (FormatException ex)
        {
            throw ParseError("Unexpected data error: " + ex.Message);
        }
    }

    private MidiTrack ReadTrack()
    {
        var tr = new MidiTrack();
        if (ReadByte() != 'M'
            || ReadByte() != 'T'
            || ReadByte() != 'r'
            || ReadByte() != 'k')
        {
            throw ParseError("MTrk is expected");
        }

        var trackSize = ReadInt32();
        current_track_size = 0;
        var total = 0;
        while (current_track_size < trackSize)
        {
            var delta = ReadVariableLength();
            tr.Messages.Add(ReadMessage(delta));
            total += delta;
        }

        if (current_track_size != trackSize)
        {
            throw ParseError("Size information mismatch");
        }

        return tr;
    }

    private int current_track_size;
    private byte running_status;

    private MidiMessage ReadMessage(int deltaTime)
    {
        var b = PeekByte();
        running_status = b < 0x80 ? running_status : ReadByte();
        switch (running_status)
        {
            case MidiEvent.SysEx1:
            case MidiEvent.SysEx2:
            case MidiEvent.Meta:
                var metaType = running_status == MidiEvent.Meta ? ReadByte() : (byte)0;
                var len = ReadVariableLength();
                var args = new byte[len];

                if (len > 0)
                {
                    ReadBytes(args);
                }

                return new MidiMessage(deltaTime, new MidiEvent(running_status, metaType, 0, args, 0, args.Length));
            default:
                int value = running_status;
                value += ReadByte() << 8;

                if (MidiEvent.FixedDataSize(running_status) == 2)
                {
                    value += ReadByte() << 16;
                }

                return new MidiMessage(deltaTime, new MidiEvent(value));
        }
    }

    private void ReadBytes(byte[] args)
    {
        current_track_size += args.Length;
        var start = 0;

        if (peek_byte >= 0)
        {
            args[0] = (byte)peek_byte;
            peek_byte = -1;
            start = 1;
        }

        var len = stream.Read(args, start, args.Length - start);
        try
        {
            if (len < args.Length - start)
            {
                throw ParseError($"The stream is insufficient to read {args.Length} bytes specified in the SMF message. Only {len} bytes read.");
            }
        }
        finally
        {
            stream_position += len;
        }
    }

    private int ReadVariableLength()
    {
        var val = 0;
        for (var i = 0; i < 4; i++)
        {
            var b = ReadByte();
            val = (val << 7) + b;
            if (b < 0x80)
            {
                return val;
            }

            val -= 0x80;
        }

        throw ParseError("Delta time specification exceeds the 4-byte limitation.");
    }

    private int peek_byte = -1;
    private int stream_position;

    private byte PeekByte()
    {
        if (peek_byte < 0)
        {
            peek_byte = stream.ReadByte();
        }

        if (peek_byte < 0)
        {
            throw ParseError("Insufficient stream. Failed to read a byte.");
        }

        return (byte)peek_byte;
    }

    private byte ReadByte()
    {
        try
        {
            current_track_size++;
            if (peek_byte >= 0)
            {
                var b = (byte)peek_byte;
                peek_byte = -1;
                return b;
            }
            var ret = stream.ReadByte();
            if (ret < 0)
            {
                throw ParseError("Insufficient stream. Failed to read a byte.");
            }

            return (byte)ret;

        }
        finally
        {
            stream_position++;
        }
    }

    private short ReadInt16() => (short)((ReadByte() << 8) + ReadByte());
    private int ReadInt32() => (((ReadByte() << 8) + ReadByte() << 8) + ReadByte() << 8) + ReadByte();
    private SmfParserException ParseError(string msg) => new(string.Format($"{msg} (at {stream_position})"));
}

public class SmfParserException(string message) : Exception(message)
{
}

public class SmfTrackMerger
{
    public static MidiMusic Merge(MidiMusic source) => new SmfTrackMerger(source).GetMergedMessages();

    private SmfTrackMerger(MidiMusic source)
    {
        this.source = source;
    }

    private readonly MidiMusic source;

    // FIXME: it should rather be implemented to iterate all
    // tracks with index to messages, pick the track which contains
    // the nearest event and push the events into the merged queue.
    // It's simpler, and costs less by removing sort operation
    // over thousands of events.
    private MidiMusic GetMergedMessages()
    {
        if (source.Format == 0)
        {
            return source;
        }

        List<MidiMessage> l = [];

        foreach (var track in source.Tracks)
        {
            var delta = 0;
            foreach (var mev in track.Messages)
            {
                delta += mev.DeltaTime;
                l.Add(new MidiMessage(delta, mev.Event));
            }
        }

        if (l.Count == 0)
        {
            return new MidiMusic() { DeltaTimeSpec = source.DeltaTimeSpec }; // empty (why did you need to sort your song file?)
        }

        // Usual Sort() over simple list of MIDI events does not work as expected.
        // For example, it does not always preserve event 
        // orders on the same channels when the delta time
        // of event B after event A is 0. It could be sorted
        // either as A->B or B->A.
        //
        // To resolve this issue, we have to sort "chunk"
        // of events, not all single events themselves, so
        // that order of events in the same chunk is preserved
        // i.e. [AB] at 48 and [CDE] at 0 should be sorted as
        // [CDE] [AB].

        var idxl = new List<int>(l.Count)
        {
            0
        };
        var prev = 0;

        for (var i = 0; i < l.Count; i++)
        {
            if (l[i].DeltaTime != prev)
            {
                idxl.Add(i);
                prev = l[i].DeltaTime;
            }
        }

        idxl.Sort(delegate (int i1, int i2)
        {
            return l[i1].DeltaTime - l[i2].DeltaTime;
        });

        // now build a new event list based on the sorted blocks.
        var l2 = new List<MidiMessage>(l.Count);
        int idx;

        for (var i = 0; i < idxl.Count; i++)
        {
            for (idx = idxl[i], prev = l[idx].DeltaTime; idx < l.Count && l[idx].DeltaTime == prev; idx++)
            {
                l2.Add(l[idx]);
            }
        }

        l = l2;

        // now messages should be sorted correctly.
        var waitToNext = l[0].DeltaTime;

        for (var i = 0; i < l.Count - 1; i++)
        {
            if (l[i].Event.Value != 0)
            {
                // if non-dummy
                var tmp = l[i + 1].DeltaTime - l[i].DeltaTime;
                l[i] = new MidiMessage(waitToNext, l[i].Event);
                waitToNext = tmp;
            }
        }

        l[^1] = new MidiMessage(waitToNext, l[^1].Event);

        var m = new MidiMusic
        {
            DeltaTimeSpec = source.DeltaTimeSpec,
            Format = 0
        };
        m.Tracks.Add(new MidiTrack(l));
        return m;
    }
}