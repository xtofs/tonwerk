using System.Numerics;

namespace Tonwerk.Tests;

public class SynthesisTests
{
    const int Rate = 44_100;

    /// <summary>Power spectrum of the first 2^16 samples (rectangular window is fine for noise and exact-bin tones).</summary>
    static double[] PowerSpectrum(Track track)
    {
        const int n = 1 << 16;
        var buffer = track.Samples[..n].ToArray().Select(s => new Complex(s, 0)).ToArray();
        FftSharp.FFT.Forward(buffer);
        return buffer.Take(n / 2).Select(c => c.Magnitude * c.Magnitude).ToArray();
    }

    static double BandEnergyDb(double[] power, double low, double high)
    {
        var binWidth = (double)Rate / (2 * power.Length);
        var sum = 0.0;
        for (var bin = (int)(low / binWidth); bin < (int)(high / binWidth); bin++) sum += power[bin];
        return 10 * Math.Log10(sum);
    }

    // energy per octave band changes by 3·(1 − α) dB per octave for power ∝ 1/f^α
    [Theory]
    [InlineData(NoiseColor.White, 3)]
    [InlineData(NoiseColor.Pink, 0)]
    [InlineData(NoiseColor.Brown, -3)]
    [InlineData(NoiseColor.Blue, 6)]
    [InlineData(NoiseColor.Violet, 9)]
    public void Noise_has_the_slope_of_its_color(NoiseColor color, double dbPerOctave)
    {
        var power = PowerSpectrum(Noise.Generate(color, 3, Rate));

        double[] edges = [250, 500, 1000, 2000, 4000];
        for (var i = 0; i + 2 < edges.Length; i++)
        {
            var step = BandEnergyDb(power, edges[i + 1], edges[i + 2]) - BandEnergyDb(power, edges[i], edges[i + 1]);
            Assert.InRange(step, dbPerOctave - 1.5, dbPerOctave + 1.5);
        }
    }

    [Fact]
    public void Noise_is_normalized_and_reproducible()
    {
        var a = Noise.Generate(NoiseColor.Pink, 1, Rate);
        var b = Noise.Generate(NoiseColor.Pink, 1, Rate);
        Assert.Equal(a.Samples.ToArray(), b.Samples.ToArray());
        Assert.Equal(1.0, a.Samples.ToArray().Max(Math.Abs), 6);
    }

    [Fact]
    public void Chord_sounds_at_the_exact_pitch_frequencies()
    {
        // 2^16 samples at 44.1 kHz: bin width 0.673 Hz; an A4 sine at 440 Hz must peak in bin 654 (440.0 / 0.6729 = 653.9)
        var chord = Chord.Major(Pitch.A4);
        var track = Synthesizer.Chord(chord, 2, Waveform.Sine, Rate);
        var power = PowerSpectrum(track);

        var binWidth = (double)Rate / (2 * power.Length);
        foreach (var pitch in chord.Pitches.ToArray())
        {
            var expected = (int)Math.Round(pitch.Frequency / binWidth);
            var peak = Enumerable.Range(expected - 3, 7).MaxBy(b => power[b]);
            Assert.Equal(expected, peak);
        }
    }

    [Fact]
    public void Chord_cannot_clip()
    {
        var track = Synthesizer.Chord(Chord.DominantSeventh(Pitch.A4), 1, Waveform.Square, Rate);
        Assert.All(track.Samples.ToArray(), s => Assert.InRange(s, -1.0, 1.0));
    }
}
