using System.Runtime.InteropServices;

namespace Tonwerk.Wav;

/// <summary>The 16-byte PCMWAVEFORMAT stored in the "fmt " chunk.</summary>
[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct WaveFormat
{
    public static WaveFormat CreatePcm(int rate, int channels = 1, int bitsPerSample = 16)
    {
        return new WaveFormat
        {
            FormatTag = 1,
            Channels = (ushort)channels,
            SamplesPerSec = (uint)rate,
            AvgBytesPerSec = (uint)(rate * channels * bitsPerSample / 8),
            BlockAlign = (ushort)(channels * bitsPerSample / 8),
            BitsPerSample = (ushort)bitsPerSample
        };
    }

    public ushort FormatTag;        // 1 = PCM
    public ushort Channels;
    public uint SamplesPerSec;
    public uint AvgBytesPerSec;
    public ushort BlockAlign;
    public ushort BitsPerSample;
}
