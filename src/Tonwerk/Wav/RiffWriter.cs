namespace Tonwerk.Wav;

using System.Buffers.Binary;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;


// RiffWriter.cs — streaming writer for RIFF files (WAV, AVI, WebP, ...).
// Target: .NET 8+, C# 12.
//
// RIFF recap:
//   chunk      = ckID:FOURCC  ckSize:uint32le  ckData[ckSize]  [pad byte if ckSize is odd]
//   RIFF/LIST  = ckID ("RIFF"/"LIST")  ckSize  formType:FOURCC  subchunk*
//   ckSize never includes the chunk's own pad byte, but a parent's ckSize
//   includes the padded size of every child.


/// <summary>
/// Forward-only RIFF writer.
/// <list type="bullet">
///   <item>Chunks whose size is known up front (<see cref="WriteChunk(FourCC, ReadOnlySpan{byte})"/>,
///   <see cref="WriteChunk{T}(FourCC, in T)"/>, <see cref="BeginChunk(FourCC, uint)"/>) work on any stream.</item>
///   <item>Chunks whose size is found at the end (<see cref="BeginChunk(FourCC)"/>, <see cref="BeginRiff"/>,
///   <see cref="BeginList"/>) need a seekable stream; their size field is patched in <see cref="EndChunk()"/>.</item>
/// </list>
/// Pad bytes for odd-sized chunks are written automatically.
/// </summary>
public sealed class RiffWriter : IDisposable
{
    private struct OpenChunk
    {
        public FourCC Id;
        public long SizeFieldOffset;   // relative to _origin
        public long DataStart;         // relative to _origin
        public uint? DeclaredSize;     // null: size gets patched at EndChunk
    }

    private readonly Stream _stream;
    private readonly bool _leaveOpen;
    private readonly long _origin;     // stream position when the writer was created (0 if not seekable)
    private readonly Stack<OpenChunk> _open = new();
    private long _position;            // bytes written by this writer
    private bool _disposed;

    public RiffWriter(Stream stream, bool leaveOpen = false)
    {
        ArgumentNullException.ThrowIfNull(stream);
        if (!stream.CanWrite) throw new ArgumentException("Stream must be writable.", nameof(stream));
        _stream = stream;
        _leaveOpen = leaveOpen;
        _origin = stream.CanSeek ? stream.Position : 0;
    }

    /// <summary>Bytes written by this writer so far.</summary>
    public long Position => _position;

    /// <summary>Number of chunks currently open.</summary>
    public int Depth => _open.Count;

    // ------------------------------------------------------------------ FourCC

    public void WriteFourCC(FourCC code)
    {
        Span<byte> b = stackalloc byte[4];
        code.CopyTo(b);
        WriteRaw(b);
    }

    // ------------------------------------------------------------------ complete chunks (size known)

    /// <summary>Writes a whole chunk: header, <paramref name="data"/>, and the pad byte if needed.</summary>
    public void WriteChunk(FourCC id, ReadOnlySpan<byte> data)
    {
        WriteHeader(id, (uint)data.Length);
        WriteRaw(data);
        if ((data.Length & 1) != 0) WriteRaw([0]);
    }

    /// <summary>
    /// Writes a fixed-size chunk whose payload is the raw bytes of <paramref name="header"/>
    /// (for example WAVEFORMATEX as the "fmt " chunk). Declare <typeparamref name="T"/> with
    /// <c>[StructLayout(LayoutKind.Sequential, Pack = 1)]</c> so no layout padding goes to disk.
    /// The chunk size is <c>sizeof(T)</c>.
    /// </summary>
    public void WriteChunk<T>(FourCC id, in T header) where T : unmanaged
    {
        EnsureLittleEndianHost();
        WriteChunk(id, MemoryMarshal.AsBytes(new ReadOnlySpan<T>(in header)));
    }

    /// <summary>Writes a chunk holding a NUL-terminated string (as used by INFO subchunks like "ISFT").</summary>
    public void WriteStringChunk(FourCC id, string text, Encoding? encoding = null)
    {
        encoding ??= Encoding.ASCII;
        byte[] bytes = new byte[encoding.GetByteCount(text) + 1];  // last byte stays 0
        encoding.GetBytes(text, 0, text.Length, bytes, 0);
        WriteChunk(id, bytes);
    }

    // ------------------------------------------------------------------ open chunks (variable length)

    /// <summary>
    /// Starts a variable-length chunk. Write its content with the Write* methods, then
    /// dispose the scope (or call <see cref="EndChunk()"/>). Needs a seekable stream.
    /// </summary>
    public ChunkScope BeginChunk(FourCC id)
    {
        RequireSeek(id);
        return Open(id, declaredSize: null);
    }

    /// <summary>
    /// Starts a chunk whose size is known in advance but whose content is written piece by piece.
    /// Works on non-seekable streams; <see cref="EndChunk()"/> checks that exactly
    /// <paramref name="size"/> bytes were written.
    /// </summary>
    public ChunkScope BeginChunk(FourCC id, uint size) => Open(id, size);

    /// <summary>Starts a "RIFF" chunk with the given form type (e.g. "WAVE", "AVI ").</summary>
    public ChunkScope BeginRiff(FourCC formType) => BeginContainer(FourCC.Riff, formType);

    /// <summary>Starts a "LIST" chunk with the given list type (e.g. "INFO", "hdrl").</summary>
    public ChunkScope BeginList(FourCC listType) => BeginContainer(FourCC.List, listType);

    private ChunkScope BeginContainer(FourCC id, FourCC type)
    {
        RequireSeek(id);
        var scope = Open(id, declaredSize: null);
        WriteFourCC(type);   // counted in the container's ckSize
        return scope;
    }

    /// <summary>Ends the innermost open chunk: sets or checks its size, then writes the pad byte if needed.</summary>
    public void EndChunk()
    {
        ThrowIfDisposed();
        if (_open.Count == 0) throw new InvalidOperationException("No chunk is open.");

        OpenChunk c = _open.Pop();
        long length = _position - c.DataStart;

        if (c.DeclaredSize is uint declared)
        {
            if (length != declared)
            {
                throw new InvalidOperationException(
                $"Chunk '{c.Id}' was declared as {declared} bytes, but {length} bytes were written.");
            }
        }
        else
        {
            if (length > uint.MaxValue)
            {
                throw new InvalidOperationException($"Chunk '{c.Id}' is larger than 4 GiB, which RIFF cannot represent.");
            }

            Span<byte> b = stackalloc byte[4];
            BinaryPrimitives.WriteUInt32LittleEndian(b, (uint)length);
            long end = _stream.Position;
            _stream.Position = _origin + c.SizeFieldOffset;
            _stream.Write(b);
            _stream.Position = end;
        }

        if ((length & 1) != 0) WriteRaw([0]);
    }

    internal void EndChunk(int expectedDepth)
    {
        if (_disposed) return; // Dispose already closed everything
        if (_open.Count != expectedDepth)
        {
            throw new InvalidOperationException(
                "Chunk scopes were ended out of order; dispose inner scopes before outer ones.");
        }
        EndChunk();
    }

    // ------------------------------------------------------------------ content

    public void Write(ReadOnlySpan<byte> data) => WriteRaw(data);

    /// <summary>Writes the raw bytes of one unmanaged value (host layout, little-endian hosts only).</summary>
    public void Write<T>(in T value) where T : unmanaged
    {
        EnsureLittleEndianHost();
        WriteRaw(MemoryMarshal.AsBytes(new ReadOnlySpan<T>(in value)));
    }

    /// <summary>Writes the raw bytes of a span of unmanaged values, e.g. PCM samples.</summary>
    public void Write<T>(ReadOnlySpan<T> values) where T : unmanaged
    {
        EnsureLittleEndianHost();
        WriteRaw(MemoryMarshal.AsBytes(values));
    }

    public void WriteByte(byte value) => WriteRaw([value]);

    public void WriteUInt16(ushort value)
    {
        Span<byte> b = stackalloc byte[2];
        BinaryPrimitives.WriteUInt16LittleEndian(b, value);
        WriteRaw(b);
    }

    public void WriteInt16(short value) => WriteUInt16((ushort)value);

    public void WriteUInt32(uint value)
    {
        Span<byte> b = stackalloc byte[4];
        BinaryPrimitives.WriteUInt32LittleEndian(b, value);
        WriteRaw(b);
    }

    public void WriteInt32(int value) => WriteUInt32((uint)value);

    public void WriteUInt64(ulong value)
    {
        Span<byte> b = stackalloc byte[8];
        BinaryPrimitives.WriteUInt64LittleEndian(b, value);
        WriteRaw(b);
    }

    // ------------------------------------------------------------------ lifetime

    public void Flush() => _stream.Flush();

    /// <summary>
    /// Ends any chunks still open (like XmlWriter closing open elements), flushes,
    /// and closes the stream unless leaveOpen was set.
    /// </summary>
    public void Dispose()
    {
        if (_disposed) return;
        try
        {
            while (_open.Count > 0) EndChunk();
            _stream.Flush();
        }
        finally
        {
            _disposed = true;
            if (!_leaveOpen) _stream.Dispose();
        }
    }

    // ------------------------------------------------------------------ helpers

    private ChunkScope Open(FourCC id, uint? declaredSize)
    {
        ThrowIfDisposed();
        WriteFourCC(id);
        long sizeField = _position;
        WriteUInt32(declaredSize ?? 0);   // placeholder if patched later
        _open.Push(new OpenChunk
        {
            Id = id,
            SizeFieldOffset = sizeField,
            DataStart = _position,
            DeclaredSize = declaredSize,
        });
        return new ChunkScope(this, _open.Count);
    }

    private void WriteHeader(FourCC id, uint size)
    {
        WriteFourCC(id);
        WriteUInt32(size);
    }

    private void WriteRaw(ReadOnlySpan<byte> data)
    {
        ThrowIfDisposed();
        _stream.Write(data);
        _position += data.Length;
    }

    private void RequireSeek(FourCC id)
    {
        if (!_stream.CanSeek)
            throw new NotSupportedException(
                $"Chunk '{id}' needs its size patched at the end, so the stream must be seekable. " +
                "On a non-seekable stream, use BeginChunk(id, size) or WriteChunk.");
    }

    private static void EnsureLittleEndianHost()
    {
        if (!BitConverter.IsLittleEndian)
            throw new PlatformNotSupportedException(
                "Raw struct writes assume a little-endian host; write fields one by one with WriteUInt16/WriteUInt32.");
    }

    private void ThrowIfDisposed() => ObjectDisposedException.ThrowIf(_disposed, this);
}
