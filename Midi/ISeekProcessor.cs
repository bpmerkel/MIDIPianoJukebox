namespace MIDIPianoJukebox.Midi;

public interface ISeekProcessor
{
    SeekFilterResult FilterMessage(MidiMessage message);
}