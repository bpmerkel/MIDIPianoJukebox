namespace MIDIPianoJukebox.Midi;

public readonly struct MidiMessage(int deltaTime, MidiEvent evt)
{
    public readonly int DeltaTime = deltaTime;
    public readonly MidiEvent Event = evt;
}
