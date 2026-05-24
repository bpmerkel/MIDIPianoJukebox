namespace Commons.Music.Midi;

public enum SeekFilterResult
{
    Pass,
    Block,
    PassAndTerminate,
    BlockAndTerminate,
}