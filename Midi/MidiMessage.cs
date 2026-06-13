namespace MIDIPianoJukebox.Midi;

/// <summary>
/// Represents a MIDI message with delta time and event data.
/// </summary>
/// <param name="deltaTime">The delta time in ticks before this message.</param>
/// <param name="evt">The MIDI event data.</param>
public readonly struct MidiMessage(int deltaTime, MidiEvent evt)
{
    /// <summary>
    /// Gets the delta time in ticks before this message.
    /// </summary>
    public readonly int DeltaTime = deltaTime;

    /// <summary>
    /// Gets the MIDI event data.
    /// </summary>
    public readonly MidiEvent Event = evt;
}
