namespace MIDIPianoJukebox.Midi;

/// <summary>
/// Processes MIDI messages during seek operations to filter events appropriately.
/// </summary>
public interface ISeekProcessor
{
    /// <summary>
    /// Filters a MIDI message during seek operation.
    /// </summary>
    /// <param name="message">The MIDI message to filter.</param>
    /// <returns>The filter result indicating whether to pass, block, or terminate.</returns>
    SeekFilterResult FilterMessage(MidiMessage message);
}