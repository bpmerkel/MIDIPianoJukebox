namespace Commons.Music.Midi;

/// <summary>
/// Used by MidiPlayer to manage time progress.
/// </summary>
public interface IMidiPlayerTimeManager
{
    void WaitBy(int addedMilliseconds);
}
