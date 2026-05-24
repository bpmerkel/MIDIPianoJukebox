namespace Commons.Music.Midi;

public interface ISeekProcessor
{
    SeekFilterResult FilterMessage(MidiMessage message);
}