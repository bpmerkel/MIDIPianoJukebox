namespace Commons.Music.Midi;

public class SimpleSeekProcessor(int ticks) : ISeekProcessor
{
    private readonly int seek_to = ticks;
    private int current;

    public SeekFilterResult FilterMessage(MidiMessage message)
    {
        current += message.DeltaTime;

        if (current >= seek_to)
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