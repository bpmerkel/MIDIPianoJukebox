namespace MIDIPianoJukebox.Midi;

/// <summary>
/// Provides General MIDI instrument and percussion mappings.
/// </summary>
public class MidiInstruments
{
    /*
        General MIDI Instrument Families
        See https://midimusic.github.io/tech/midispec.html#BM3_
        The General MIDI instrument sounds are grouped by families.
        In each family, there are 8 specific instruments.
        PC#	    Family
    */
    public static readonly (byte min, byte max, string family)[] GeneralMidiFamilies = """
        1-8 Piano
        9-16 Chromatic Percussion
        17-24 Organ
        25-32 Guitar
        33-40 Bass
        41-48 Strings
        49-56 Ensemble
        57-64 Brass
        65-72 Reed
        73-80 Pipe
        81-88 Synth Lead
        89-96 Synth Pad
        97-104 Synth Effects
        105-112 Ethnic
        113-120 Percussive
        121-128 Sound Effects
        """
        .Split(["\r\n", "\r", "\n"], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
        .Select(line =>
        {
            var parts = line.Split([' ', '\t'], 2, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            var range = parts[0].Split('-');
            return (min: (byte)(byte.Parse(range[0])-1), max: (byte)(byte.Parse(range[1])-1), family: parts[1]);
        })
        .ToArray();

    public static string Family(byte id) => GeneralMidiFamilies
        .Where(f => f.min <= id && id <= f.max)
        .Select(f => f.family)
        .FirstOrDefault();

    /*
        General MIDI Instrument Patch Map
        While General MIDI does not define the actual characteristics of any sounds,
        the names in parentheses after each of the synth leads, pads, and sound effects are, in particular,
        intended only as guides.
        PC#	Instrument
    */
    public static readonly Dictionary<byte, string> GeneralMidiInstruments = """
        1.	Acoustic Grand Piano
        2.	Bright Acoustic Piano
        3.	Electric Grand Piano
        4.	Honky-tonk Piano
        5.	Electric Piano 1 (Rhodes Piano)
        6.	Electric Piano 2 (Chorused Piano)
        7.	Harpsichord
        8.	Clavinet
        9.	Celesta
        10.	Glockenspiel
        11.	Music Box
        12.	Vibraphone
        13.	Marimba
        14.	Xylophone
        15.	Tubular Bells
        16.	Dulcimer (Santur)
        17.	Drawbar Organ (Hammond)
        18.	Percussive Organ
        19.	Rock Organ
        20.	Church Organ
        21.	Reed Organ
        22.	Accordion (French)
        23.	Harmonica
        24.	Tango Accordion (Band neon)
        25.	Acoustic Guitar (nylon)
        26.	Acoustic Guitar (steel)
        27.	Electric Guitar (jazz)
        28.	Electric Guitar (clean)
        29.	Electric Guitar (muted)
        30.	Overdriven Guitar
        31.	Distortion Guitar
        32.	Guitar harmonics
        33.	Acoustic Bass
        34.	Electric Bass (fingered)
        35.	Electric Bass (picked)
        36.	Fretless Bass
        37.	Slap Bass 1
        38.	Slap Bass 2
        39.	Synth Bass 1
        40.	Synth Bass 2
        41.	Violin
        42.	Viola
        43.	Cello
        44.	Contrabass
        45.	Tremolo Strings
        46.	Pizzicato Strings
        47.	Orchestral Harp
        48.	Timpani
        49.	String Ensemble 1 (strings)
        50.	String Ensemble 2 (slow strings)
        51.	SynthStrings 1
        52.	SynthStrings 2
        53.	Choir Aahs
        54.	Voice Oohs
        55.	Synth Voice
        56.	Orchestra Hit
        57.	Trumpet
        58.	Trombone
        59.	Tuba
        60.	Muted Trumpet
        61.	French Horn
        62.	Brass Section
        63.	SynthBrass 1
        64.	SynthBrass 2
        65.	Soprano Sax
        66. Alto Sax
        67.	Tenor Sax
        68. Baritone Sax
        69.	Oboe
        70.	English Horn
        71. Bassoon
        72.	Clarinet
        73.	Piccolo
        74.	Flute
        75.	Recorder
        76.	Pan Flute
        77.	Blown Bottle
        78.	Shakuhachi
        79.	Whistle
        80.	Ocarina
        81.	Lead 1 (square wave)
        82.	Lead 2 (sawtooth wave)
        83.	Lead 3 (calliope)
        84.	Lead 4 (chiffer)
        85.	Lead 5 (charang)
        86.	Lead 6 (voice solo)
        87.	Lead 7 (fifths)
        88.	Lead 8 (bass + lead)
        89.	Pad 1 (new age Fantasia)
        90.	Pad 2 (warm)
        91.	Pad 3 (polysynth)
        92.	Pad 4 (choir space voice)
        93.	Pad 5 (bowed glass)
        94.	Pad 6 (metallic pro)
        95.	Pad 7 (halo)
        96.	Pad 8 (sweep)
        97.	FX 1 (rain)
        98.	FX 2 (soundtrack)
        99.	FX 3 (crystal)
        100.	FX 4 (atmosphere)
        101.	FX 5 (brightness)
        102.	FX 6 (goblins)
        103.	FX 7 (echoes, drops)
        104.	FX 8 (sci-fi, star theme)
        105.	Sitar
        106.	Banjo
        107.	Shamisen
        108.	Koto
        109.	Kalimba
        110.	Bag pipe
        111.	Fiddle
        112.	Shanai
        113.	Tinkle Bell
        114.	Agogo
        115.	Steel Drums
        116.	Woodblock
        117.	Taiko Drum
        118.	Melodic Tom
        119.	Synth Drum
        120.	Reverse Cymbal
        121.	Guitar Fret Noise
        122.	Breath Noise
        123.	Seashore
        124.	Bird Tweet
        125.	Telephone Ring
        126.	Helicopter
        127.	Applause
        128.	Gunshot
        """
        .Split(["\r\n", "\r", "\n"], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
        .Select(line =>
        {
            var parts = line.Split([' ', '\t'], 2, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            return (id: (byte)(byte.Parse(parts[0].TrimEnd('.'))-1), name: parts[1]);
        })
        .ToDictionary(x => x.id, x => x.name);

    public static string Instrument(byte id) => GeneralMidiInstruments.TryGetValue(id, out var name) ? name : null;

    /*
        General MIDI Percussion Key Map
        On MIDI Channel 10, each MIDI Note number ("Key#") corresponds to a different drum sound.
        General MIDI-compatible instruments must have the sounds on the keys shown here.
        While many current instruments also have additional sounds above or below the range show here,
        and may even have additional "kits" with variations of these sounds,
        only these sounds are supported by General MIDI.
        Key#	Note	Drum Sound
    */
    public static readonly Dictionary<byte, (string note, string instrument)> GeneralMidiPercussion = """
        35	B1	Acoustic Bass Drum
        36	C2	Bass Drum 1
        37	C#2	Side Stick
        38	D2	Acoustic Snare
        39	D#2	Hand Clap
        40	E2	Electric Snare
        41	F2	Low Floor Tom
        42	F#2	Closed Hi Hat
        43	G2	High Floor Tom
        44	G#2	Pedal Hi-Hat
        45	A2	Low Tom
        46	A#2	Open Hi-Hat
        47	B2	Low-Mid Tom
        48	C3	Hi Mid Tom
        49	C#3	Crash Cymbal 1
        50	D3	High Tom
        51	D#3	Ride Cymbal 1
        52	E3	Chinese Cymbal
        53	F3	Ride Bell
        54	F#3	Tambourine
        55	G3	Splash Cymbal
        56	G#3	Cowbell
        57	A3	Crash Cymbal 2
        58	A#3	Vibraslap
        59	B3	Ride Cymbal 2
        60	C4	Hi Bongo
        61	C#4	Low Bongo
        62	D4	Mute Hi Conga
        63	D#4	Open Hi Conga
        64	E4	Low Conga
        65	F4	High Timbale
        66	F#4	Low Timbale
        67	G4	High Agogo
        68	G#4	Low Agogo
        69	A4	Cabasa
        70	A#4	Maracas
        71	B4	Short Whistle
        72	C5	Long Whistle
        73	C#5	Short Guiro
        74	D5	Long Guiro
        75	D#5	Claves
        76	E5	Hi Wood Block
        77	F5	Low Wood Block
        78	F#5	Mute Cuica
        79	G5	Open Cuica
        80	G#5	Mute Triangle
        81	A5	Open Triangle
        """
        .Split(["\r\n", "\r", "\n"], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
        .Select(line =>
        {
            var parts = line.Split([' ', '\t'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            return (id: (byte)(byte.Parse(parts[0]) - 1), note: parts[1], instrument: string.Join(" ", parts.Skip(2)));
        })
        .ToDictionary(x => x.id, x => (x.note, x.instrument));

    public static (string note, string instrument) Percussion(byte id) => GeneralMidiPercussion.TryGetValue(id, out var value) ? value : (null, null);
}