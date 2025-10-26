﻿namespace Commons.Music.Midi.WinMM;

public class WinMMMidiOutput : IMidiOutput
{
    public WinMMMidiOutput(IMidiPortDetails details)
    {
        Details = details;
        _ = WinMMNatives.midiOutOpen(out handle, uint.Parse(Details.Id), null, IntPtr.Zero, MidiOutOpenFlags.Null);
        Connection = MidiPortConnectionState.Open;
    }

    private readonly IntPtr handle;
    public IMidiPortDetails Details { get; private set; }
    public MidiPortConnectionState Connection { get; private set; }

    public Task CloseAsync()
    {
        return Task.Run(() =>
        {
            Connection = MidiPortConnectionState.Pending;
            _ = WinMMNatives.midiOutClose(handle);
            Connection = MidiPortConnectionState.Closed;
        });
    }

    public void Dispose()
    {
        CloseAsync().Wait();
    }

    public void Send(byte[] mevent, int offset, int length, long timestamp)
    {
        foreach (var evt in MidiEvent.Convert(mevent, offset, length))
        {
            if (evt.StatusByte < 0xF0 || evt.ExtraData == null)
            {
                DieOnError(WinMMNatives.midiOutShortMsg(handle, (uint)(evt.StatusByte + (evt.Msb << 8) + (evt.Lsb << 16))));
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

                    DieOnError(WinMMNatives.midiOutPrepareHeader(handle, ptr, hdrSize));
                    prepared = true;

                    DieOnError(WinMMNatives.midiOutLongMsg(handle, ptr, hdrSize));
                }
                finally
                {
                    // reclaim ownership and free
                    if (prepared)
                    {
                        DieOnError(WinMMNatives.midiOutUnprepareHeader(handle, ptr, hdrSize));
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
