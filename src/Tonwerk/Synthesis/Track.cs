namespace Tonwerk.Synthesis;

/// <summary>
/// A mono audio track: samples in [-1, 1] at a fixed sample rate.
/// Wrapping the raw samples ties them to their rate — two tracks can only be
/// mixed, and a track only written, when the rates match.
/// </summary>
public readonly struct Track(double[] samples, int rate)
{
    /// <summary>The samples in [-1, 1] (empty for the default instance).</summary>
    public ReadOnlySpan<double> Samples => samples;

    /// <summary>Samples per second.</summary>
    public int Rate { get; } = rate;

    public int Length => samples?.Length ?? 0;

    public TimeSpan Duration => TimeSpan.FromSeconds(Length / (double)Rate);

    /// <summary>
    /// Mixes two tracks by addition (superposition) — combining simultaneous
    /// sounds is simply adding their samples. The weights sum to 1, so two
    /// tracks in [-1, 1] cannot clip. The shorter track is padded with silence.
    /// </summary>
    public Track Mix(Track other, double weight = 0.5)
    {
        if (Rate != other.Rate)
            throw new InvalidOperationException($"Cannot mix tracks with different sample rates: {Rate} vs {other.Rate}");

        var otherWeight = 1.0 - weight;
        var mine = Samples;
        var theirs = other.Samples;
        var mixed = new double[Math.Max(mine.Length, theirs.Length)];
        for (var i = 0; i < mixed.Length; i++)
        {
            var a = i < mine.Length ? mine[i] : 0.0;
            var b = i < theirs.Length ? theirs[i] : 0.0;
            mixed[i] = weight * a + otherWeight * b;
        }
        return new Track(mixed, Rate);
    }

    /// <summary>A track of silence.</summary>
    public static Track Silence(double seconds, int rate) => new(new double[(int)(rate * seconds)], rate);

    /// <summary>Places tracks one after another on the timeline.</summary>
    public static Track Concat(int rate, IEnumerable<Track> tracks)
    {
        var list = tracks.ToList();
        var samples = new double[list.Sum(t => t.Length)];
        var position = 0;
        foreach (var track in list)
        {
            if (track.Rate != rate)
                throw new InvalidOperationException($"Cannot concatenate tracks with different sample rates: {rate} vs {track.Rate}");
            track.Samples.CopyTo(samples.AsSpan(position));
            position += track.Length;
        }
        return new Track(samples, rate);
    }

    /// <summary>
    /// Adds simultaneous tracks and divides by their count, so tracks in [-1, 1]
    /// cannot clip. The shorter tracks are padded with silence.
    /// </summary>
    public static Track Sum(int rate, IEnumerable<Track> tracks)
    {
        var list = tracks.ToList();
        var samples = new double[list.Count == 0 ? 0 : list.Max(t => t.Length)];
        foreach (var track in list)
        {
            if (track.Rate != rate)
                throw new InvalidOperationException($"Cannot mix tracks with different sample rates: {rate} vs {track.Rate}");
            var source = track.Samples;
            for (var i = 0; i < source.Length; i++) samples[i] += source[i] / list.Count;
        }
        return new Track(samples, rate);
    }
}
