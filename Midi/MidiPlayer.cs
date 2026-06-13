namespace MIDIPianoJukebox.Midi;

public delegate void MidiEventAction(MidiEvent m);

/// <summary>
/// Provides asynchronous MIDI player control.
/// </summary>
public class MidiPlayer : IAsyncDisposable
{
    private const int MidiChannelCount = 16;
    private const int DefaultBufferSize = 0x100;

    public MidiPlayer(MidiMusic music, IMidiOutput output)
    {
        ArgumentNullException.ThrowIfNull(music);
        ArgumentNullException.ThrowIfNull(output);

        var timeManager = new SimpleAdjustingMidiPlayerTimeManager();
        _music = music;
        _output = output;

        _messages = SmfTrackMerger.Merge(music).Tracks[0].Messages;
        _player = new MidiEventLooper(_messages, timeManager, music.DeltaTimeSpec);
        _player.Starting += () =>
        {
            // all control reset on all channels.
            for (var i = 0; i < MidiChannelCount; i++)
            {
                _buffer[0] = (byte)(i + 0xB0);
                _buffer[1] = 0x79;
                _buffer[2] = 0;
                _output.Send(_buffer, 0, 3, 0);
            }
        };

        EventReceived += (m) =>
        {
            switch (m.EventType)
            {
                case MidiEvent.SysEx1:
                case MidiEvent.SysEx2:
                    if (_buffer.Length <= m.ExtraDataLength)
                    {
                        _buffer = new byte[_buffer.Length * 2];
                    }

                    _buffer[0] = m.StatusByte;
                    Array.Copy(m.ExtraData!, m.ExtraDataOffset, _buffer, 1, m.ExtraDataLength);
                    _output.Send(_buffer, 0, m.ExtraDataLength + 1, 0);
                    break;
                case MidiEvent.Meta:
                    // do nothing.
                    break;
                case MidiEvent.NoteOn:
                case MidiEvent.NoteOff:
                default:
                    var size = MidiEvent.FixedDataSize(m.StatusByte);
                    _buffer[0] = m.StatusByte;
                    _buffer[1] = m.Msb;
                    _buffer[2] = m.Lsb;
                    _output.Send(_buffer, 0, size + 1, 0);
                    break;
            }
        };
    }

    private readonly MidiEventLooper _player;
    private Task _syncPlayerTask;
    private readonly IMidiOutput _output;
    private readonly IList<MidiMessage> _messages;
    private readonly MidiMusic _music;
    private byte[] _buffer = new byte[DefaultBufferSize];

    public event Action PlaybackCompletedToEnd
    {
        add { _player.PlaybackCompletedToEnd += value; }
        remove { _player.PlaybackCompletedToEnd -= value; }
    }

    public PlayerState State => _player._state;

    public int Tempo => _player._currentTempo;
    // You can break the data at your own risk but I take performance precedence.
    public int PlayDeltaTime => _player._playDeltaTime;
    public TimeSpan PositionInTime => TimeSpan.FromMilliseconds(_music.GetTimePositionInMillisecondsForTick(PlayDeltaTime));
    public int GetTotalPlayTimeMilliseconds() => MidiMusic.GetTotalPlayTimeMilliseconds(_messages, _music.DeltaTimeSpec);

    public event MidiEventAction EventReceived
    {
        add { _player.EventReceived += value; }
        remove { _player.EventReceived -= value; }
    }

    public virtual async ValueTask DisposeAsync()
    {
        await _player.DisposeAsync();
        _output.Dispose();
        GC.SuppressFinalize(this);
    }

    public async Task PlayAsync()
    {
        switch (State)
        {
            case PlayerState.Playing:
                return; // do nothing
            case PlayerState.Paused:
                await _player.PlayAsync();
                return;
            case PlayerState.Stopped:
                if (_syncPlayerTask == null || _syncPlayerTask.Status != TaskStatus.Running)
                {
                    _syncPlayerTask = Task.Run(async () => { await _player.PlayerLoopAsync(); });
                }
                await _player.PlayAsync();
                return;
        }
    }

    public async Task PauseAsync()
    {
        if (State == PlayerState.Playing)
        {
            await _player.PauseAsync();
        }
    }

    public async Task StopAsync()
    {
        switch (State)
        {
            case PlayerState.Paused:
            case PlayerState.Playing:
                await _player.StopAsync();
                break;
        }
    }

    public async Task SeekAsync(int ticks) => await _player.SeekAsync(null, ticks);
}