namespace MIDIPianoJukebox.Midi;

/// <summary>
/// Represents a MIDI music file with tracks and timing information.
/// </summary>
public class MidiMusic
{
    /// <summary>
    /// Reads a MIDI file from a stream.
    /// </summary>
    /// <param name="stream">The stream containing MIDI file data.</param>
    /// <returns>A MidiMusic instance representing the parsed MIDI file.</returns>
    public static MidiMusic Read(Stream stream)
    {
        var r = new SmfReader();
        r.Read(stream);
        return r.Music;
    }

    private readonly List<MidiTrack> tracks = [];

    public MidiMusic()
    {
        Format = 1;
    }

    /// <summary>
    /// Gets or sets the delta time specification (ticks per quarter note or SMPTE format).
    /// </summary>
    public short DeltaTimeSpec { get; set; }

    /// <summary>
    /// Gets or sets the MIDI file format (0, 1, or 2).
    /// </summary>
    public byte Format { get; set; }

    /// <summary>
    /// Gets the list of MIDI tracks in this music.
    /// </summary>
    public IList<MidiTrack> Tracks => tracks;

    public string[] GetInstruments() => Tracks
        .SelectMany(t => t.Messages)
        .Select(m => m.Event)
        .Where(e => (e.EventType & 0xF0) == MidiEvent.Program)
        .Select(e => MidiInstruments.Instrument(e.Msb)) // MidiInstruments.Family(e.Msb)
        .Distinct()
        .OrderBy(i => i)
        .ToArray();

    public IEnumerable<MidiMessage> GetMetaEventsOfType(byte metaType) => Format != 0
        ? SmfTrackMerger.Merge(this).GetMetaEventsOfType(metaType)
        : GetMetaEventsOfType(tracks[0].Messages, metaType);

    public static IEnumerable<MidiMessage> GetMetaEventsOfType(IEnumerable<MidiMessage> messages, byte metaType)
    {
        var v = 0;
        foreach (var m in messages)
        {
            v += m.DeltaTime;
            if (m.Event.EventType == MidiEvent.Meta && m.Event.Msb == metaType)
            {
                yield return new MidiMessage(v, m.Event);
            }
        }
    }

    public int GetTotalTicks() => Format != 0
        ? SmfTrackMerger.Merge(this).GetTotalTicks()
        : Tracks[0].Messages.Sum(m => m.DeltaTime);

    public int GetTotalPlayTimeMilliseconds() => Format != 0
        ? SmfTrackMerger.Merge(this).GetTotalPlayTimeMilliseconds()
        : GetTotalPlayTimeMilliseconds(Tracks[0].Messages, DeltaTimeSpec);

    public int GetTimePositionInMillisecondsForTick(int ticks) => Format != 0
        ? SmfTrackMerger.Merge(this).GetTimePositionInMillisecondsForTick(ticks)
        : GetPlayTimeMillisecondsAtTick(Tracks[0].Messages, ticks, DeltaTimeSpec);

    public static int GetTotalPlayTimeMilliseconds(IList<MidiMessage> messages, int deltaTimeSpec) => GetPlayTimeMillisecondsAtTick(messages, messages.Sum(m => m.DeltaTime), deltaTimeSpec);

    private static int GetPlayTimeMillisecondsAtTick(IList<MidiMessage> messages, int ticks, int deltaTimeSpec)
    {
        if (deltaTimeSpec < 0)
        {
            throw new NotSupportedException("non-tick based DeltaTime");
        }

        var tempo = MidiMetaType.DefaultTempo;
        var t = 0;
        var v = 0d;

        foreach (var m in messages)
        {
            var deltaTime = t + m.DeltaTime < ticks ? m.DeltaTime : ticks - t;
            v += (double)tempo / 1000 * deltaTime / deltaTimeSpec;

            if (deltaTime != m.DeltaTime)
            {
                break;
            }

            t += m.DeltaTime;

            if (m.Event.EventType == MidiEvent.Meta && m.Event.Msb == MidiMetaType.Tempo)
            {
                tempo = MidiMetaType.GetTempo(m.Event.ExtraData, m.Event.ExtraDataOffset);
            }
        }
        return (int)v;
    }
}