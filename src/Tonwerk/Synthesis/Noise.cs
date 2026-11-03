namespace Tonwerk.Synthesis;

/// <summary>
/// Generates the colors of noise in the time domain, sample by sample, by
/// filtering white noise. Unlike block-based spectral synthesis the output
/// never repeats and can have any length. The slopes are close approximations
/// of the ideal 1/f^α (see the tests); the exact spectral construction lives
/// in the noise app.
/// </summary>
public static class Noise
{
    /// <summary>
    /// Noise of the given color, normalized to peak 1. Seeded, so the same
    /// arguments always give the same samples.
    /// </summary>
    public static Track Generate(NoiseColor color, double seconds, int rate, int seed = 42)
    {
        var rng = new Random(seed);
        var count = (int)(rate * seconds);
        var samples = new double[count];

        switch (color)
        {
            case NoiseColor.White:
                for (var i = 0; i < count; i++) samples[i] = White(rng);
                break;

            case NoiseColor.Pink:
                FillPink(samples, rng);
                break;

            case NoiseColor.Brown:
                // leaky integration of white noise: -6 dB/octave above the ~140 Hz leak corner
                var brown = 0.0;
                for (var i = 0; i < count; i++)
                {
                    brown = (brown + 0.02 * White(rng)) / 1.02;
                    samples[i] = brown;
                }
                break;

            case NoiseColor.Blue:
                // differentiating (+6 dB/octave) pink noise (-3 dB/octave) leaves +3 dB/octave
                FillPink(samples, rng);
                Differentiate(samples);
                break;

            case NoiseColor.Violet:
                for (var i = 0; i < count; i++) samples[i] = White(rng);
                Differentiate(samples);
                break;

            default:
                throw new ArgumentOutOfRangeException(nameof(color), color, null);
        }

        Normalize(samples);
        return new Track(samples, rate);
    }

    private static double White(Random rng) => 2 * rng.NextDouble() - 1;

    /// <summary>
    /// Pink noise with Paul Kellet's "refined" filter: a bank of one-pole
    /// low-passes whose sum approximates -3 dB/octave within about ±0.05 dB
    /// over the audible range.
    /// </summary>
    private static void FillPink(double[] samples, Random rng)
    {
        double b0 = 0, b1 = 0, b2 = 0, b3 = 0, b4 = 0, b5 = 0, b6 = 0;
        for (var i = 0; i < samples.Length; i++)
        {
            var white = White(rng);
            b0 = 0.99886 * b0 + white * 0.0555179;
            b1 = 0.99332 * b1 + white * 0.0750759;
            b2 = 0.96900 * b2 + white * 0.1538520;
            b3 = 0.86650 * b3 + white * 0.3104856;
            b4 = 0.55000 * b4 + white * 0.5329522;
            b5 = -0.7616 * b5 - white * 0.0168980;
            samples[i] = b0 + b1 + b2 + b3 + b4 + b5 + b6 + white * 0.5362;
            b6 = white * 0.115926;
        }
    }

    /// <summary>First difference y[n] = x[n] − x[n−1]: +6 dB/octave.</summary>
    private static void Differentiate(double[] samples)
    {
        var previous = 0.0;
        for (var i = 0; i < samples.Length; i++)
        {
            var current = samples[i];
            samples[i] = current - previous;
            previous = current;
        }
    }

    private static void Normalize(double[] samples)
    {
        var peak = 0.0;
        foreach (var s in samples) peak = Math.Max(peak, Math.Abs(s));
        if (peak == 0) return;
        for (var i = 0; i < samples.Length; i++) samples[i] /= peak;
    }
}
