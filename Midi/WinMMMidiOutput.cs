namespace MIDIPianoJukebox.Midi;

/// <summary>
/// Windows Multimedia MIDI output implementation.
/// </summary>
public class WinMMMidiOutput : IMidiOutput
{
    private readonly IntPtr _handle;

    public WinMMMidiOutput(IMidiPortDetails details)
    {
        Details = details;
        _ = WinMMNatives.midiOutOpen(out _handle, uint.Parse(Details.Id), null, IntPtr.Zero, MidiOutOpenFlags.Null);
        Connection = MidiPortConnectionState.Open;
    }

    public IMidiPortDetails Details { get; private set; }
    public MidiPortConnectionState Connection { get; private set; }

    public Task CloseAsync()
    {
        return Task.Run(() =>
        {
            Connection = MidiPortConnectionState.Pending;
            _ = WinMMNatives.midiOutClose(_handle);
            Connection = MidiPortConnectionState.Closed;
        });
    }

    public void Dispose()
    {
        if (Connection != MidiPortConnectionState.Closed)
        {
            Connection = MidiPortConnectionState.Pending;
            _ = WinMMNatives.midiOutClose(_handle);
            Connection = MidiPortConnectionState.Closed;
        }
    }

    public void Send(byte[] mevent, int offset, int length, long timestamp)
    {
        foreach (var evt in MidiEvent.Convert(mevent, offset, length))
        {
            if (evt.StatusByte < 0xF0 || evt.ExtraData == null)
            {
                DieOnError(WinMMNatives.midiOutShortMsg(_handle, (uint)(evt.StatusByte + (evt.Msb << 8) + (evt.Lsb << 16))));
            }
            else
            {
                var header = new MidiHdr();
                var prepared = false;
                IntPtr ptr = IntPtr.Zero;
                var hdrSize = Marshal.SizeOf<MidiHdr>();

                try
                {
                    // allocate unmanaged memory and hand ownership over to the device driver
                    header.Data = Marshal.AllocHGlobal(evt.ExtraDataLength);
                    header.BufferLength = evt.ExtraDataLength;
                    Marshal.Copy(evt.ExtraData, evt.ExtraDataOffset, header.Data, header.BufferLength);

                    ptr = Marshal.AllocHGlobal(hdrSize);
                    Marshal.StructureToPtr(header, ptr, false);

                    DieOnError(WinMMNatives.midiOutPrepareHeader(_handle, ptr, hdrSize));
                    prepared = true;

                    DieOnError(WinMMNatives.midiOutLongMsg(_handle, ptr, hdrSize));
                }
                finally
                {
                    // reclaim ownership and free
                    if (prepared)
                    {
                        DieOnError(WinMMNatives.midiOutUnprepareHeader(_handle, ptr, hdrSize));
                    }

                    if (header.Data != IntPtr.Zero)
                    {
                        Marshal.FreeHGlobal(header.Data);
                    }

                    if (ptr != IntPtr.Zero)
                    {
                        Marshal.FreeHGlobal(ptr);
                    }
                }
            }
        }
    }

    private static void DieOnError(int code)
    {
        if (code != 0)
        {
            throw new Win32Exception(code, $"{WinMMNatives.GetMidiOutErrorText(code)} ({code})");
        }
    }
}