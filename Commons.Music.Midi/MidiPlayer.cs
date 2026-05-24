namespace Commons.Music.Midi;

public delegate void MidiEventAction(MidiEvent m);

// Provides asynchronous player control.
public class MidiPlayer : IDisposable
{
    public MidiPlayer(MidiMusic music, IMidiOutput output)
    {
        ArgumentNullException.ThrowIfNull(music);
        ArgumentNullException.ThrowIfNull(output);

        var timeManager = new SimpleAdjustingMidiPlayerTimeManager();
        this.music = music;
        this.output = output;

        messages = SmfTrackMerger.Merge(music).Tracks[0].Messages;
        player = new MidiEventLooper(messages, timeManager, music.DeltaTimeSpec);
        player.Starting += () =>
        {
            // all control reset on all channels.
            for (var i = 0; i < 16; i++)
            {
                buffer[0] = (byte)(i + 0xB0);
                buffer[1] = 0x79;
                buffer[2] = 0;
                output.Send(buffer, 0, 3, 0);
            }
        };

        EventReceived += (m) =>
        {
            switch (m.EventType)
            {
                case MidiEvent.SysEx1:
                case MidiEvent.SysEx2:
                    if (buffer.Length <= m.ExtraDataLength)
                    {
                        buffer = new byte[buffer.Length * 2];
                    }

                    buffer[0] = m.StatusByte;
                    Array.Copy(m.ExtraData, m.ExtraDataOffset, buffer, 1, m.ExtraDataLength);
                    output.Send(buffer, 0, m.ExtraDataLength + 1, 0);
                    break;
                case MidiEvent.Meta:
                    // do nothing.
                    break;
                case MidiEvent.NoteOn:
                case MidiEvent.NoteOff:
                default:
                    var size = MidiEvent.FixedDataSize(m.StatusByte);
                    buffer[0] = m.StatusByte;
                    buffer[1] = m.Msb;
                    buffer[2] = m.Lsb;
                    output.Send(buffer, 0, size + 1, 0);
                    break;
            }
        };
    }

    private readonly MidiEventLooper player;
    private Task sync_player_task;
    private readonly IMidiOutput output;
    private readonly IList<MidiMessage> messages;
    private readonly MidiMusic music;
    private byte[] buffer = new byte[0x100];

    public event Action PlaybackCompletedToEnd
    {
        add { player.PlaybackCompletedToEnd += value; }
        remove { player.PlaybackCompletedToEnd -= value; }
    }

    public PlayerState State => player.state;

    public int Tempo => player.current_tempo;
    // You can break the data at your own risk but I take performance precedence.
    public int PlayDeltaTime => player.play_delta_time;
    public TimeSpan PositionInTime => TimeSpan.FromMilliseconds(music.GetTimePositionInMillisecondsForTick(PlayDeltaTime));
    public int GetTotalPlayTimeMilliseconds() => MidiMusic.GetTotalPlayTimeMilliseconds(messages, music.DeltaTimeSpec);

    public event MidiEventAction EventReceived
    {
        add { player.EventReceived += value; }
        remove { player.EventReceived -= value; }
    }

    public virtual void Dispose()
    {
        player.Stop();
        output.Dispose();
        GC.SuppressFinalize(this);
    }

    public void Play()
    {
        switch (State)
        {
            case PlayerState.Playing:
                return; // do nothing
            case PlayerState.Paused:
                player.Play();
                return;
            case PlayerState.Stopped:
                if (sync_player_task == null || sync_player_task.Status != TaskStatus.Running)
                {
                    sync_player_task = Task.Run(() => { player.PlayerLoop(); });
                }
                player.Play();
                return;
        }
    }

    public void Pause()
    {
        if (State == PlayerState.Playing)
        {
            player.Pause();
        }
    }

    public void Stop()
    {
        switch (State)
        {
            case PlayerState.Paused:
            case PlayerState.Playing:
                player.Stop();
                break;
        }
    }

    public void Seek(int ticks) => player.Seek(null, ticks);
}