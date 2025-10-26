namespace Commons.Music.Midi;

public class MidiTrack
{
    public IList<MidiMessage> Messages { init; get; }

    public MidiTrack()
        : this([])
    {
    }

    public MidiTrack(IList<MidiMessage> messages)
    {
        ArgumentNullException.ThrowIfNull(messages);
        Messages = messages as List<MidiMessage> ?? [.. messages];
    }
}
