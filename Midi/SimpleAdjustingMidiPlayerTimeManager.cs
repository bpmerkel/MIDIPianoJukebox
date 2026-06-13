namespace MIDIPianoJukebox.Midi;

/// <summary>
/// Simple time manager that adjusts MIDI playback timing to maintain synchronization.
/// </summary>
public class SimpleAdjustingMidiPlayerTimeManager : IMidiPlayerTimeManager
{
    private DateTime _lastStarted = default;
    private long _nominalTotalMilliseconds = 0L;

    public void WaitBy(int addedMilliseconds)
    {
        if (addedMilliseconds > 0)
        {
            long delta = addedMilliseconds;

            if (_lastStarted != default)
            {
                var actualTotalMilliseconds = (long)(DateTime.Now - _lastStarted).TotalMilliseconds;
                delta -= actualTotalMilliseconds - _nominalTotalMilliseconds;
            }
            else
            {
                _lastStarted = DateTime.Now;
            }

            if (delta > 0)
            {
                var t = Task.Delay((int)delta);
                t.Wait();
            }

            _nominalTotalMilliseconds += addedMilliseconds;
        }
    }
}