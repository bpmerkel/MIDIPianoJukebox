namespace MIDIPianoJukebox.Midi;

/// <summary>
/// Exception thrown when an error occurs while parsing a Standard MIDI File (SMF).
/// </summary>
/// <param name="message">The error message that explains the reason for the exception.</param>
public class SmfParserException(string message) : Exception(message)
{
}
