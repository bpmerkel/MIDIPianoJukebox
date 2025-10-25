namespace Commons.Music.Midi.WinMM;

[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
public struct MidiOutCaps
{
    public short Mid;
    public short Pid;
    public int DriverVersion;
    [MarshalAs(UnmanagedType.ByValTStr, SizeConst = WinMMNatives.MaxPNameLen)]
    public string Name;
    public short Technology;
    public short Voices;
    public short Notes;
    public short ChannelMask;
    public int Support;
}

[StructLayout(LayoutKind.Sequential)]
public struct MidiHdr
{
    public IntPtr Data;
    public int BufferLength;
    public int BytesRecorded;
    public IntPtr User;
    public int Flags;
    public IntPtr Next; // of MidiHdr
    public IntPtr Reserved;
    public int Offset;
    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
    private readonly int[] reservedArray;
}

[Flags]
public enum MidiOutOpenFlags
{
    Null,
    Function,
    Thread,
    Window,
    Event,
}

public delegate void MidiOutProc(IntPtr midiOut, uint msg, IntPtr instance, IntPtr param1, IntPtr param2);

public static partial class WinMMNatives
{
    public const string LibraryName = "winmm";
    public const int MaxPNameLen = 32;

    [LibraryImport(LibraryName)]
    internal static partial int midiOutGetNumDevs();

    [DllImport(LibraryName)]
    internal static extern int midiOutGetDevCaps(UIntPtr uDeviceID, out MidiOutCaps midiOutCaps, uint sizeOfMidiOutCaps);

    [LibraryImport(LibraryName)]
    internal static partial int midiOutOpen(out IntPtr midiIn, uint deviceID, MidiOutProc callback, IntPtr callbackInstance, MidiOutOpenFlags flags);

    [LibraryImport(LibraryName)]
    internal static partial int midiOutClose(IntPtr midiIn);

    [LibraryImport(LibraryName)]
    internal static partial int midiOutShortMsg(IntPtr handle, uint msg);

    [LibraryImport(LibraryName)]
    internal static partial int midiOutLongMsg(IntPtr handle, IntPtr midiOutHdr, int midiOutHdrSize);

    [LibraryImport(LibraryName)]
    internal static partial int midiOutPrepareHeader(IntPtr handle, IntPtr midiOutHdr, int midiOutHdrSize);

    [LibraryImport(LibraryName)]
    internal static partial int midiOutUnprepareHeader(IntPtr handle, IntPtr headerPtr, int sizeOfMidiHeader);

    [DllImport(LibraryName, CharSet = CharSet.Unicode)]
    internal static extern int midiOutGetErrorText(int mmrError, StringBuilder message, int sizeOfMessage);

    internal static string GetMidiOutErrorText(int code, int maxLength = 128)
    {
        var errorMsg = new StringBuilder(maxLength);
        return midiOutGetErrorText(code, errorMsg, maxLength) == 0
            ? errorMsg.ToString()
            : "Unknown winmm midi output error";
    }
}