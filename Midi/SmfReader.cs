namespace MIDIPianoJukebox.Midi;

/// <summary>
/// Standard MIDI File (SMF) reader for parsing MIDI files.
/// </summary>
public class SmfReader
{
    private const int MaxVariableLengthBytes = 4;

    private Stream _stream;
    private MidiMusic _data;
    public MidiMusic Music => _data!;

    public void Read(Stream stream)
    {
        ArgumentNullException.ThrowIfNull(stream);

        _stream = stream;
        _data = new MidiMusic();
        try
        {
            DoParse();
        }
        finally
        {
            _stream = null;
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

        _data!.Format = (byte)ReadInt16();
        var tracks = ReadInt16();
        _data.DeltaTimeSpec = ReadInt16();
        try
        {
            for (var i = 0; i < tracks; i++)
            {
                _data.Tracks.Add(ReadTrack());
            }
        }
        catch (FormatException ex)
        {
            throw ParseError("Unexpected data error: " + ex.Message);
        }
    }

    private MidiTrack ReadTrack()
    {
        if (ReadByte() != 'M'
            || ReadByte() != 'T'
            || ReadByte() != 'r'
            || ReadByte() != 'k')
        {
            throw ParseError("MTrk is expected");
        }

        var trackSize = ReadInt32();
        _currentTrackSize = 0;
        var total = 0;
        var tr = new MidiTrack();

        while (_currentTrackSize < trackSize)
        {
            var delta = ReadVariableLength();
            tr.Messages.Add(ReadMessage(delta));
            total += delta;
        }

        if (_currentTrackSize != trackSize)
        {
            throw ParseError("Size information mismatch");
        }

        return tr;
    }

    private int _currentTrackSize;
    private byte _runningStatus;

    private MidiMessage ReadMessage(int deltaTime)
    {
        var b = PeekByte();
        _runningStatus = b < 0x80 ? _runningStatus : ReadByte();
        switch (_runningStatus)
        {
            case MidiEvent.SysEx1:
            case MidiEvent.SysEx2:
            case MidiEvent.Meta:
                var metaType = _runningStatus == MidiEvent.Meta ? ReadByte() : (byte)0;
                var len = ReadVariableLength();
                var args = new byte[len];

                if (len > 0)
                {
                    ReadBytes(args);
                }

                return new MidiMessage(deltaTime, new MidiEvent(_runningStatus, metaType, 0, args, 0, args.Length));

            default:
                int value = _runningStatus;
                value += ReadByte() << 8;

                if (MidiEvent.FixedDataSize(_runningStatus) == 2)
                {
                    value += ReadByte() << 16;
                }

                return new MidiMessage(deltaTime, new MidiEvent(value));
        }
    }

    private void ReadBytes(byte[] args)
    {
        _currentTrackSize += args.Length;
        var start = 0;

        if (_peekByte >= 0)
        {
            args[0] = (byte)_peekByte;
            _peekByte = -1;
            start = 1;
        }

        var len = _stream!.Read(args, start, args.Length - start);
        try
        {
            if (len < args.Length - start)
            {
                throw ParseError($"The stream is insufficient to read {args.Length} bytes specified in the SMF message. Only {len} bytes read.");
            }
        }
        finally
        {
            _streamPosition += len;
        }
    }

    private int ReadVariableLength()
    {
        var val = 0;

        for (var i = 0; i < MaxVariableLengthBytes; i++)
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

    private int _peekByte = -1;
    private int _streamPosition;

    private byte PeekByte()
    {
        if (_peekByte < 0)
        {
            _peekByte = _stream!.ReadByte();
        }

        if (_peekByte < 0)
        {
            throw ParseError("Insufficient stream. Failed to read a byte.");
        }

        return (byte)_peekByte;
    }

    private byte ReadByte()
    {
        try
        {
            _currentTrackSize++;

            if (_peekByte >= 0)
            {
                var b = (byte)_peekByte;
                _peekByte = -1;
                return b;
            }

            var ret = _stream!.ReadByte();

            if (ret < 0)
            {
                throw ParseError("Insufficient stream. Failed to read a byte.");
            }

            return (byte)ret;

        }
        finally
        {
            _streamPosition++;
        }
    }

    private short ReadInt16() => (short)((ReadByte() << 8) + ReadByte());
    private int ReadInt32() => (((ReadByte() << 8) + ReadByte() << 8) + ReadByte() << 8) + ReadByte();
    private SmfParserException ParseError(string msg) => new($"{msg} (at {_streamPosition})");
}
