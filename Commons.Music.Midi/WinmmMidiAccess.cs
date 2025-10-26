namespace Commons.Music.Midi.WinMM;

public class WinMMMidiAccess : IMidiAccess
{
    public IEnumerable<IMidiPortDetails> Outputs
    {
        get
        {
            var devs = WinMMNatives.midiOutGetNumDevs();
            for (uint i = 0; i < devs; i++)
            {
                var err = WinMMNatives.midiOutGetDevCaps((UIntPtr)i, out MidiOutCaps caps, (uint)Marshal.SizeOf<MidiOutCaps>());

                if (err != 0)
                {
                    throw new Win32Exception(err);
                }

                yield return new WinMMPortDetails(i, caps.Name, caps.DriverVersion);
            }
        }
    }

    public Task<IMidiOutput> OpenOutputAsync(string portId)
    {
        var details = Outputs.FirstOrDefault(d => d.Id == portId);
        return details == null
            ? throw new InvalidOperationException($"The device with ID {portId} is not found.")
            : Task.FromResult((IMidiOutput)new WinMMMidiOutput(details));
    }
}
