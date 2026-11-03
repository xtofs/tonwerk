using System.Numerics;

namespace Tonwerk.Noise;

/// <summary>
/// Noise built in the frequency domain: every audible bin gets a magnitude
/// following the 1/f^α power envelope and a uniformly random phase; an inverse
/// FFT turns the spectrum into samples. (A flat envelope with coherent phases
/// would be a click; the random phases make it noise.) The result is one
/// block that repeats if played longer — and the FFT is the only reason this
/// app references FftSharp.
/// </summary>
static class SpectralNoise
{
    const int N = 1 << 17;          // FFT size: ~3 s at 44.1 kHz
    const double LowCutoff = 20.0;  // skip sub-audible bins

    public static Track Generate(NoiseColor color, int rate, Random rng)
    {
        var alpha = Alpha(color);
        var binWidth = (double)rate / N;
        var spectrum = new Complex[N];

        for (var bin = 1; bin < N / 2; bin++) // DC and Nyquist stay 0
        {
            var frequency = bin * binWidth;
            if (frequency < LowCutoff) continue;

            // power ∝ f^-α, so magnitude ∝ f^(-α/2)
            var magnitude = Math.Pow(frequency, -alpha / 2.0);
            var component = Complex.FromPolarCoordinates(magnitude, 2 * Math.PI * rng.NextDouble());
            spectrum[bin] = component;
            spectrum[N - bin] = Complex.Conjugate(component); // conjugate symmetry: real signal
        }

        FftSharp.FFT.Inverse(spectrum);

        var peak = spectrum.Max(c => Math.Abs(c.Real));
        var samples = spectrum.Select(c => c.Real / peak).ToArray();
        return new Track(samples, rate);
    }

    static double Alpha(NoiseColor color) => color switch
    {
        NoiseColor.White => 0,
        NoiseColor.Pink => 1,
        NoiseColor.Brown => 2,
        NoiseColor.Blue => -1,
        NoiseColor.Violet => -2,
        _ => throw new ArgumentOutOfRangeException(nameof(color), color, null),
    };
}
