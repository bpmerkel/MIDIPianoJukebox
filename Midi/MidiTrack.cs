namespace MIDIPianoJukebox.Midi;

/// <summary>
/// Represents a MIDI track containing a sequence of MIDI messages.
/// </summary>
public class MidiTrack
{
    public IList<MidiMessage> Messages { get; init; }

    public MidiTrack()
        : this([])
    {
    }

    public MidiTrack(IList<MidiMessage> messages)
    {
        ArgumentNullException.ThrowIfNull(messages);
        Messages = messages;
    }
}