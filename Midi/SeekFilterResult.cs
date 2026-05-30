namespace MIDIPianoJukebox.Midi;

public enum SeekFilterResult
{
    Pass,
    Block,
    PassAndTerminate,
    BlockAndTerminate,
}