namespace Commons.Music.Midi;

public abstract class MidiModuleDatabase
{
    public static readonly MidiModuleDatabase Default = new DefaultMidiModuleDatabase();
    public abstract IEnumerable<MidiModuleDefinition> All();
    public abstract MidiModuleDefinition Resolve(string moduleName);
}

public class MergedMidiModuleDatabase(IEnumerable<MidiModuleDatabase> sources) : MidiModuleDatabase
{
    public IList<MidiModuleDatabase> List { get; private set; } = [];
    public override IEnumerable<MidiModuleDefinition> All() => List.Concat(sources).SelectMany(d => d.All());
    public override MidiModuleDefinition Resolve(string moduleName) => List.Select(d => d.Resolve(moduleName)).FirstOrDefault(m => m != null);
}

class DefaultMidiModuleDatabase : MidiModuleDatabase
{
    static readonly Assembly ass = typeof(DefaultMidiModuleDatabase).GetTypeInfo().Assembly;
    // am too lazy to adjust resource names :/
    public static Stream GetResource(string name) => ass.GetManifestResourceStream(ass.GetManifestResourceNames().FirstOrDefault(m => m.EndsWith(name, StringComparison.OrdinalIgnoreCase)));

    public DefaultMidiModuleDatabase()
    {
        Modules = [];
        var catalog = new StreamReader(GetResource("midi-module-catalog.txt")).ReadToEnd().Split('\n');
        // strip extraneous \r
        foreach (var filename in catalog.Select(s => s.Trim()).Where(filename => filename.Length > 0))
        {
            Modules.Add(MidiModuleDefinition.Load(GetResource(filename)));
        }
    }

    public override IEnumerable<MidiModuleDefinition> All() => Modules;

    public override MidiModuleDefinition Resolve(string moduleName)
    {
        if (moduleName == null)
        {
            return null;
        }

        var name = ResolvePossibleAlias(moduleName);
        return Modules.FirstOrDefault(m => m.Name == name) ?? Modules.FirstOrDefault(m => m.Match != null && new Regex(m.Match).IsMatch(name) || name.Contains(m.Name));
    }

    public static string ResolvePossibleAlias(string name) => name switch
    {
        "Microsoft GS Wavetable Synth" => "Microsoft GS Wavetable SW Synth",
        _ => name,
    };

    public IList<MidiModuleDefinition> Modules { get; private set; }
}

[DataContract]
public class MidiModuleDefinition
{
    public MidiModuleDefinition()
    {
        Instrument = new MidiInstrumentDefinition();
    }

    [DataMember]
    public string Name { get; set; }

    [DataMember]
    public string Match { get; set; }

    [DataMember]
    public MidiInstrumentDefinition Instrument { get; set; }

    // serialization

    public void Save(Stream stream)
    {
        var ds = new DataContractJsonSerializer(typeof(MidiModuleDefinition));
        ds.WriteObject(stream, this);
    }

    public static MidiModuleDefinition Load(Stream stream)
    {
        var ds = new DataContractJsonSerializer(typeof(MidiModuleDefinition));
        return (MidiModuleDefinition)ds.ReadObject(stream);
    }
}

[DataContract]
public class MidiInstrumentDefinition
{
    public MidiInstrumentDefinition()
    {
        Maps = [];
        DrumMaps = [];
    }

    [DataMember(Name = "Maps")]
    public IList<MidiInstrumentMap> Maps { get; private set; }

    [DataMember(Name = "DrumMaps")]
    public IList<MidiInstrumentMap> DrumMaps { get; private set; }
}

[DataContract]
public class MidiInstrumentMap
{
    public MidiInstrumentMap()
    {
        Programs = [];
    }

    [DataMember]
    public string Name { get; set; }

    [DataMember(Name = "Programs")]
    public IList<MidiProgramDefinition> Programs { get; private set; }
}

[DataContract]
public class MidiProgramDefinition
{
    public MidiProgramDefinition()
    {
        Banks = [];
    }

    [DataMember]
    public string Name { get; set; }
    [DataMember]
    public int Index { get; set; }

    [DataMember(Name = "Banks")]
    public IList<MidiBankDefinition> Banks { get; private set; }
}

[DataContract]
public class MidiBankDefinition
{
    [DataMember]
    public string Name { get; set; }
    [DataMember]
    public int Msb { get; set; }
    [DataMember]
    public int Lsb { get; set; }
}
