namespace Tonwerk.Wav;



/// <summary>
/// Writes WAV files using the RIFF format.
/// Supports writing PCM audio data to a WAV file.
/// Example usage:
/// <code>
/// using var writer = new WavWriter("output.wav", 44100);
/// writer.Write(samples);
/// </code>
/// </summary>
public class WavWriter : IDisposable
{
    private readonly RiffWriter _riffWriter;
    private readonly Stack<ChunkScope?> _scopes;
    private bool _disposed;

    public int Rate { get; }

    public WavWriter(string filePath, int rate)
    {
        Rate = rate;
        _scopes = new Stack<ChunkScope?>();

        var stream = File.Create(filePath);
        _riffWriter = new RiffWriter(stream);

        _scopes.Push(_riffWriter.BeginRiff("WAVE"));

        // Fixed-size chunk from a struct
        _riffWriter.WriteChunk("fmt ", WaveFormat.CreatePcm(rate, 1, 16));

        // Variable-length chunk: size patched when the scope ends
        _scopes.Push(_riffWriter.BeginChunk("data"));
    }

    /// <summary>
    /// Writes a <see cref="Track"/> at the given volume. The track's sample
    /// rate must match the writer's.
    /// </summary>
    public void Write(Track track, double volume = 1.0)
    {
        if (track.Rate != Rate)
            throw new InvalidOperationException($"Track rate {track.Rate} does not match writer rate {Rate}");
        Write(track.Samples, volume);
    }

    /// <summary>
    /// Writes a track of samples in [-1, 1], scaled by <paramref name="volume"/>,
    /// as 16-bit PCM. Values outside the range are clamped. This is the single
    /// place where floating-point audio is converted to integer PCM — all
    /// processing code operates on doubles.
    /// </summary>
    public void Write(ReadOnlySpan<double> track, double volume = 1.0)
    {
        var buffer = Pool.Rent(track.Length);
        for (var i = 0; i < track.Length; i++)
        {
            buffer[i] = (short)(short.MaxValue * Math.Clamp(track[i] * volume, -1.0, 1.0));
        }
        _riffWriter.Write(buffer.AsSpan(0, track.Length));
        Pool.Return(buffer);
    }

    /// <summary>Writes raw 16-bit PCM samples.</summary>
    private void Write(ReadOnlySpan<short> data)
    {
        _riffWriter.Write(data);
    }

    private static readonly System.Buffers.ArrayPool<short> Pool = System.Buffers.ArrayPool<short>.Shared;

    public void Dispose()
    {
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (_disposed)
            return;
        if (disposing)
        {
            while (_scopes.TryPop(out var scope))
            {
                scope?.Dispose();
            }
            _riffWriter.Dispose();
        }
        _disposed = true;
    }
}