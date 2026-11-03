namespace Tonwerk.Wav;

using System;
using System.Buffers.Binary;
using System.Runtime.InteropServices;

/// <summary>
/// A four-character code. Stored as the little-endian uint32 that has the same
/// byte sequence as the code on disk ("RIFF" → 0x46464952).
/// </summary>
[StructLayout(LayoutKind.Sequential, Size = 4)]
public readonly struct FourCC : IEquatable<FourCC>
{
    public static readonly FourCC Riff = new("RIFF");
    public static readonly FourCC List = new("LIST");

    /// <summary>The code as a little-endian uint32 (first character in the low byte).</summary>
    public readonly uint Value;

    public FourCC(uint value) => Value = value;

    /// <summary>
    /// Creates a FourCC from 1–4 printable ASCII characters. Codes shorter than
    /// 4 characters are padded on the right with spaces, as the RIFF spec requires ("fmt" → "fmt ").
    /// </summary>
    public FourCC(ReadOnlySpan<char> code)
    {
        if (code.Length is 0 or > 4)
            throw new ArgumentException("A FourCC must have 1 to 4 characters.", nameof(code));

        uint v = 0;
        for (int i = 0; i < 4; i++)
        {
            char c = i < code.Length ? code[i] : ' ';
            if (c < 0x20 || c > 0x7E)
                throw new ArgumentException($"FourCC character U+{(int)c:X4} is not printable ASCII.", nameof(code));
            v |= (uint)c << (8 * i);
        }
        Value = v;
    }

    /// <summary>Convenience conversion so call sites can write <c>w.WriteChunk("fmt ", ...)</c>.
    /// Throws for invalid codes.</summary>
    public static implicit operator FourCC(string code) => new(code.AsSpan());

    public void CopyTo(Span<byte> destination) => BinaryPrimitives.WriteUInt32LittleEndian(destination, Value);

    public override string ToString() =>
        string.Create(4, Value, static (span, v) =>
        {
            for (int i = 0; i < 4; i++) span[i] = (char)((v >> (8 * i)) & 0xFF);
        });

    public bool Equals(FourCC other) => Value == other.Value;
    public override bool Equals(object? obj) => obj is FourCC f && Equals(f);
    public override int GetHashCode() => (int)Value;
    public static bool operator ==(FourCC a, FourCC b) => a.Value == b.Value;
    public static bool operator !=(FourCC a, FourCC b) => a.Value != b.Value;
}
