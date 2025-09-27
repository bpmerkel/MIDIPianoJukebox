﻿
namespace Commons.Music.Midi.WinMM;

public class WinMMMidiAccess : IMidiAccess
{
    public IEnumerable<IMidiPortDetails> Inputs
    {
        get
        {
            var devs = WinMMNatives.midiInGetNumDevs();
            for (uint i = 0; i < devs; i++)
            {
                _ = WinMMNatives.midiInGetDevCaps(i, out MidiInCaps caps, (uint)Marshal.SizeOf<MidiInCaps>());
                yield return new WinMMPortDetails(i, caps.Name, caps.DriverVersion);
            }
        }
    }

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

    public MidiAccessExtensionManager ExtensionManager => throw new NotImplementedException();

    public Task<IMidiInput> OpenInputAsync(string portId)
    {
        var details = Inputs.FirstOrDefault(d => d.Id == portId);
        return details == null
            ? throw new InvalidOperationException($"The device with ID {portId} is not found.")
            : Task.FromResult((IMidiInput)new WinMMMidiInput(details));
    }

    public Task<IMidiOutput> OpenOutputAsync(string portId)
    {
        var details = Outputs.FirstOrDefault(d => d.Id == portId);
        return details == null
            ? throw new InvalidOperationException($"The device with ID {portId} is not found.")
            : Task.FromResult((IMidiOutput)new WinMMMidiOutput(details));
    }
}

class WinMMPortDetails(uint deviceId, string name, int version) : IMidiPortDetails
{
    public string Id { get; private set; } = deviceId.ToString();

    public string Manufacturer { get; private set; }

    public string Name { get; private set; } = name;

    public string Version { get; private set; } = version.ToString();
}

class WinMMMidiInput : IMidiInput
{
    private readonly MidiInProc midiInProc;

    public WinMMMidiInput(IMidiPortDetails details)
    {
        Details = details;

        // prevent garbage collection of the delegate
        midiInProc = HandleMidiInProc;

        DieOnError(WinMMNatives.midiInOpen(out handle, uint.Parse(Details.Id), midiInProc,
            IntPtr.Zero, MidiInOpenFlags.Function | MidiInOpenFlags.MidiIoStatus));

        DieOnError(WinMMNatives.midiInStart(handle));

        while (lmBuffers.Count < LONG_BUFFER_COUNT)
        {
            var buffer = new LongMessageBuffer(handle);

            buffer.PrepareHeader();
            buffer.AddBuffer();

            lmBuffers.Add(buffer.Ptr, buffer);
        }

        Connection = MidiPortConnectionState.Open;
    }

    const int LONG_BUFFER_COUNT = 16;

    private readonly Dictionary<IntPtr, LongMessageBuffer> lmBuffers = new();

    private readonly IntPtr handle;
    private readonly Lock lockObject = new();

    private readonly byte[] data1b = new byte[1];
    private readonly byte[] data2b = new byte[2];
    private readonly byte[] data3b = new byte[3];

    void HandleData(IntPtr param1, IntPtr param2)
    {
        var status = (byte)((int)param1 & 0xFF);
        var msb = (byte)(((int)param1 & 0xFF00) >> 8);
        var lsb = (byte)(((int)param1 & 0xFF0000) >> 16);
        var size = MidiEvent.FixedDataSize(status);
        var data = size == 1 ? data2b : size == 2 ? data3b : data1b;
        data[0] = status;
        if (data.Length >= 2)
        {
            data[1] = msb;
        }

        if (data.Length >= 3)
        {
            data[2] = lsb;
        }

        MessageReceived(this, new MidiReceivedEventArgs() { Data = data, Start = 0, Length = data.Length, Timestamp = (long)param2 });
    }

    void HandleLongData(IntPtr param1, IntPtr param2)
    {
        byte[] data = null;

        lock (lockObject)
        {
            var buffer = lmBuffers[param1];
            // FIXME: this is a nasty workaround for https://github.com/atsushieno/managed-midi/issues/49
            // We have no idea when/how this message is sent (midi in proc is not well documented).
            if (buffer.Header.BytesRecorded == 0)
            {
                if (Connection != MidiPortConnectionState.Open)
                {
                    FreeBuffer(buffer);
                }
                return;
            }

            data = new byte[buffer.Header.BytesRecorded];

            Marshal.Copy(buffer.Header.Data, data, 0, buffer.Header.BytesRecorded);

            if (Connection == MidiPortConnectionState.Open)
            {
                buffer.Recycle();
            }
            else
            {
                FreeBuffer(buffer);
            }
        }

        if (data != null && data.Length != 0)
        {
            MessageReceived(this, new MidiReceivedEventArgs() { Data = data, Start = 0, Length = data.Length, Timestamp = (long)param2 });
        }
    }

    void HandleMidiInProc(IntPtr midiIn, MidiInMessage msg, IntPtr instance, IntPtr param1, IntPtr param2)
    {
        if (MessageReceived != null)
        {
            switch (msg)
            {
                case MidiInMessage.Data:
                    HandleData(param1, param2);
                    break;

                case MidiInMessage.LongData:
                    HandleLongData(param1, param2);
                    break;

                case MidiInMessage.MoreData:
                    // TODO input too slow, handle.
                    break;

                case MidiInMessage.Error:
                    throw new InvalidOperationException($"Invalid MIDI message: {param1}");

                case MidiInMessage.LongError:
                    throw new InvalidOperationException("Invalid SysEx message.");

                default:
                    break;
            }
        }
        else if (Connection != MidiPortConnectionState.Open && msg == MidiInMessage.LongData)
        {
            lock (lockObject)
            {
                var buffer = lmBuffers[param1];
                FreeBuffer(buffer);
            }
        }
    }

    void FreeBuffer(LongMessageBuffer buffer)
    {
        lmBuffers.Remove(buffer.Ptr);
        buffer.Dispose();
    }

    public IMidiPortDetails Details { get; private set; }

    public MidiPortConnectionState Connection { get; private set; }

    public event EventHandler<MidiReceivedEventArgs> MessageReceived;

    public Task CloseAsync()
    {
        return Task.Run(() =>
        {
            lock (lockObject)
            {
                Connection = MidiPortConnectionState.Pending;

                DieOnError(WinMMNatives.midiInReset(handle));
                DieOnError(WinMMNatives.midiInStop(handle));
                DieOnError(WinMMNatives.midiInClose(handle));
            }

            // wait for the device driver to hand back the long buffers through HandleMidiInProc

            for (int i = 0; i < 1000; i++)
            {
                lock (lockObject)
                {
                    if (lmBuffers.Count < 1)
                    {
                        break;
                    }
                }

                Thread.Sleep(10);
            }

            Connection = MidiPortConnectionState.Closed;
        });
    }

    public void Dispose()
    {
        CloseAsync().Wait();
    }

    static void DieOnError(int code)
    {
        if (code != 0)
        {
            throw new Win32Exception(code, $"{WinMMNatives.GetMidiInErrorText(code)} ({code})");
        }
    }

    class LongMessageBuffer : IDisposable
    {
        public IntPtr Ptr { get; set; } = IntPtr.Zero;
        public MidiHdr Header => Marshal.PtrToStructure<MidiHdr>(Ptr);

        private readonly IntPtr inputHandle;
        private static readonly int midiHdrSize = Marshal.SizeOf<MidiHdr>();

        bool prepared = false;

        public LongMessageBuffer(IntPtr inputHandle, int bufferSize = 4096)
        {
            this.inputHandle = inputHandle;

            var header = new MidiHdr()
            {
                Data = Marshal.AllocHGlobal(bufferSize),
                BufferLength = bufferSize,
            };

            try
            {
                Ptr = Marshal.AllocHGlobal(Marshal.SizeOf<MidiHdr>());
                Marshal.StructureToPtr(header, Ptr, false);
            }
            catch
            {
                Free();
                throw;
            }
        }

        public void PrepareHeader()
        {
            if (!prepared)
            {
                DieOnError(WinMMNatives.midiInPrepareHeader(inputHandle, Ptr, midiHdrSize));
            }

            prepared = true;
        }

        public void UnPrepareHeader()
        {
            if (prepared)
            {
                DieOnError(WinMMNatives.midiInUnprepareHeader(inputHandle, Ptr, midiHdrSize));
            }

            prepared = false;
        }

        public void AddBuffer() =>
            DieOnError(WinMMNatives.midiInAddBuffer(inputHandle, Ptr, midiHdrSize));

        public void Dispose()
        {
            Free();
        }

        public void Recycle()
        {
            UnPrepareHeader();
            PrepareHeader();
            AddBuffer();
        }

        void Free()
        {
            UnPrepareHeader();

            if (Ptr != IntPtr.Zero)
            {
                Marshal.FreeHGlobal(Header.Data);
                Marshal.FreeHGlobal(Ptr);
            }
        }
    }
}

class WinMMMidiOutput : IMidiOutput
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
                bool prepared = false;
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

    static void DieOnError(int code)
    {
        if (code != 0)
        {
            throw new Win32Exception(code, $"{WinMMNatives.GetMidiOutErrorText(code)} ({code})");
        }
    }
}
