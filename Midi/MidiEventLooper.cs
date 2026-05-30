namespace MIDIPianoJukebox.Midi;

// Event loop implementation.
public class MidiEventLooper : IDisposable
{
    public MidiEventLooper(IList<MidiMessage> messages, IMidiPlayerTimeManager timeManager, int deltaTimeSpec)
    {
        ArgumentNullException.ThrowIfNull(messages);

        if (deltaTimeSpec < 0)
        {
            throw new NotSupportedException("SMPTe-based delta time is not implemented in this player.");
        }

        delta_time_spec = deltaTimeSpec;
        time_manager = timeManager;
        this.messages = messages;
        state = PlayerState.Stopped;
    }

    public MidiEventAction EventReceived;
    public event Action Starting;
    public event Action Finished;
    public event Action PlaybackCompletedToEnd;
    private readonly IMidiPlayerTimeManager time_manager;
    private readonly IList<MidiMessage> messages;
    private readonly int delta_time_spec;
    private readonly ManualResetEvent pause_handle = new(false);
    private bool do_pause, do_stop;
    internal double tempo_ratio = 1.0;
    internal PlayerState state;
    private int event_idx = 0;
    internal int current_tempo = MidiMetaType.DefaultTempo;
    internal byte[] current_time_signature = new byte[4];
    internal int play_delta_time;

    public virtual void Dispose()
    {
        if (state != PlayerState.Stopped)
        {
            Stop();
        }

        Mute();
    }

    public void Play()
    {
        pause_handle.Set();
        state = PlayerState.Playing;
    }

    private void Mute()
    {
        for (var i = 0; i < 16; i++)
        {
            OnEvent(new MidiEvent((byte)(i + 0xB0), 0x78, 0, null, 0, 0));
        }
    }

    public void Pause()
    {
        do_pause = true;
        Mute();
    }

    public void PlayerLoop()
    {
        Starting?.Invoke();
        event_idx = 0;
        play_delta_time = 0;

        while (true)
        {
            pause_handle.WaitOne();

            if (do_stop)
            {
                break;
            }

            if (do_pause)
            {
                pause_handle.Reset();
                do_pause = false;
                state = PlayerState.Paused;
                continue;
            }

            if (event_idx == messages.Count)
            {
                break;
            }

            ProcessMessage(messages[event_idx++]);
        }

        do_stop = false;
        Mute();
        state = PlayerState.Stopped;

        if (event_idx == messages.Count)
        {
            PlaybackCompletedToEnd?.Invoke();
        }

        Finished?.Invoke();
    }

    private int GetContextDeltaTimeInMilliseconds(int deltaTime) => (int)(current_tempo / 1000 * deltaTime / delta_time_spec / tempo_ratio);

    private void ProcessMessage(MidiMessage m)
    {
        if (seek_processor != null)
        {
            var result = seek_processor.FilterMessage(m);
            switch (result)
            {
                case SeekFilterResult.PassAndTerminate:
                    seek_processor = null;
                    break;
                case SeekFilterResult.BlockAndTerminate:
                    seek_processor = null;
                    return; // ignore this event
                case SeekFilterResult.Block:
                    return; // ignore this event
            }
        }
        else if (m.DeltaTime != 0)
        {
            var ms = GetContextDeltaTimeInMilliseconds(m.DeltaTime);
            time_manager.WaitBy(ms);
            play_delta_time += m.DeltaTime;
        }

        if (m.Event.StatusByte == 0xFF)
        {
            if (m.Event.Msb == MidiMetaType.Tempo)
            {
                current_tempo = MidiMetaType.GetTempo(m.Event.ExtraData, m.Event.ExtraDataOffset);
            }
            else if (m.Event.Msb == MidiMetaType.TimeSignature && m.Event.ExtraDataLength == 4)
            {
                Array.Copy(m.Event.ExtraData, current_time_signature, 4);
            }
        }

        OnEvent(m.Event);
    }

    private void OnEvent(MidiEvent m) => EventReceived?.Invoke(m);

    public void Stop()
    {
        if (state != PlayerState.Stopped)
        {
            do_stop = true;
            pause_handle?.Set();
            Finished?.Invoke();
        }
    }

    private ISeekProcessor seek_processor;

    // not sure about the interface, so make it non-public yet.
    internal void Seek(ISeekProcessor seekProcessor, int ticks)
    {
        seek_processor = seekProcessor ?? new SimpleSeekProcessor(ticks);
        event_idx = 0;
        play_delta_time = ticks;
        Mute();
    }
}