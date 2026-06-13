namespace MIDIPianoJukebox.Midi;

/// <summary>
/// Simple implementation of seek processor that filters out note events until the target tick is reached.
/// </summary>
public class SimpleSeekProcessor(int ticks) : ISeekProcessor
{
    private readonly int _seekTo = ticks;
    private int _current;

    public SeekFilterResult FilterMessage(MidiMessage message)
    {
        _current += message.DeltaTime;

        if (_current >= _seekTo)
        {
            return SeekFilterResult.PassAndTerminate;
        }

        return message.Event.EventType switch
        {
            MidiEvent.NoteOn or MidiEvent.NoteOff => SeekFilterResult.Block,
            _ => SeekFilterResult.Pass,
        };
    }
}