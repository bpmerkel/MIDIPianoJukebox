namespace MIDIPianoJukebox.Midi;

/// <summary>
/// Event loop implementation for MIDI playback.
/// </summary>
public class MidiEventLooper : IAsyncDisposable
{
    private const int MidiChannelCount = 16;

    public MidiEventLooper(IList<MidiMessage> messages, IMidiPlayerTimeManager timeManager, int deltaTimeSpec)
    {
        ArgumentNullException.ThrowIfNull(messages);

        if (deltaTimeSpec < 0)
        {
            throw new NotSupportedException("SMPTe-based delta time is not implemented in this player.");
        }

        _deltaTimeSpec = deltaTimeSpec;
        _timeManager = timeManager;
        _messages = messages;
        _state = PlayerState.Stopped;
    }

    public MidiEventAction EventReceived;
    public event Action Starting;
    public event Action Finished;
    public event Action PlaybackCompletedToEnd;
    private readonly IMidiPlayerTimeManager _timeManager;
    private readonly IList<MidiMessage> _messages;
    private readonly int _deltaTimeSpec;
    private readonly ManualResetEvent _pauseHandle = new(false);
    private bool _doPause, _doStop;
    internal double _tempoRatio = 1.0;
    internal PlayerState _state;
    private int _eventIdx = 0;
    internal int _currentTempo = MidiMetaType.DefaultTempo;
    internal byte[] _currentTimeSignature = new byte[4];
    internal int _playDeltaTime;

    public virtual async ValueTask DisposeAsync()
    {
        if (_state != PlayerState.Stopped)
        {
            await StopAsync();
        }

        await MuteAsync();
        _pauseHandle?.Dispose();
        GC.SuppressFinalize(this);
    }

    public async Task PlayAsync()
    {
        _pauseHandle.Set();
        _state = PlayerState.Playing;
    }

    private async Task MuteAsync()
    {
        for (var i = 0; i < MidiChannelCount; i++)
        {
            OnEvent(new MidiEvent((byte)(i + 0xB0), 0x78, 0, null, 0, 0));
        }
    }

    public async Task PauseAsync()
    {
        _doPause = true;
        await MuteAsync();
    }

    public async Task PlayerLoopAsync()
    {
        Starting?.Invoke();
        _eventIdx = 0;
        _playDeltaTime = 0;

        while (true)
        {
            _pauseHandle.WaitOne();

            if (_doStop)
            {
                break;
            }

            if (_doPause)
            {
                _pauseHandle.Reset();
                _doPause = false;
                _state = PlayerState.Paused;
                continue;
            }

            if (_eventIdx == _messages.Count)
            {
                break;
            }

            ProcessMessage(_messages[_eventIdx++]);
        }

        _doStop = false;
        await MuteAsync();
        _state = PlayerState.Stopped;

        if (_eventIdx == _messages.Count)
        {
            PlaybackCompletedToEnd?.Invoke();
        }

        Finished?.Invoke();
    }

    private int GetContextDeltaTimeInMilliseconds(int deltaTime) => (int)(_currentTempo / 1000 * deltaTime / _deltaTimeSpec / _tempoRatio);

    private void ProcessMessage(MidiMessage m)
    {
        if (_seekProcessor != null)
        {
            var result = _seekProcessor.FilterMessage(m);
            switch (result)
            {
                case SeekFilterResult.PassAndTerminate:
                    _seekProcessor = null;
                    break;
                case SeekFilterResult.BlockAndTerminate:
                    _seekProcessor = null;
                    return; // ignore this event
                case SeekFilterResult.Block:
                    return; // ignore this event
            }
        }
        else if (m.DeltaTime != 0)
        {
            var ms = GetContextDeltaTimeInMilliseconds(m.DeltaTime);
            _timeManager.WaitBy(ms);
            _playDeltaTime += m.DeltaTime;
        }

        if (m.Event.StatusByte == 0xFF)
        {
            if (m.Event.Msb == MidiMetaType.Tempo)
            {
                _currentTempo = MidiMetaType.GetTempo(m.Event.ExtraData, m.Event.ExtraDataOffset);
            }
            else if (m.Event.Msb == MidiMetaType.TimeSignature && m.Event.ExtraDataLength == 4)
            {
                Array.Copy(m.Event.ExtraData, _currentTimeSignature, 4);
            }
        }

        OnEvent(m.Event);
    }

    private void OnEvent(MidiEvent m) => EventReceived?.Invoke(m);

    public async Task StopAsync()
    {
        if (_state != PlayerState.Stopped)
        {
            _doStop = true;
            _pauseHandle?.Set();
            Finished?.Invoke();
        }
    }

    private ISeekProcessor _seekProcessor;

    /// <summary>
    /// Seeks to a specific position in ticks.
    /// </summary>
    internal async Task SeekAsync(ISeekProcessor seekProcessor, int ticks)
    {
        _seekProcessor = seekProcessor ?? new SimpleSeekProcessor(ticks);
        _eventIdx = 0;
        _playDeltaTime = ticks;
        await MuteAsync();
    }
}