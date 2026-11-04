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

public class EnvelopeTests
{
    const int Rate = 44_100;

    static double Peak(ReadOnlySpan<double> samples)
    {
        var peak = 0.0;
        foreach (var s in samples) peak = Math.Max(peak, Math.Abs(s));
        return peak;
    }

    [Fact]
    public void Default_tone_is_sustained()
    {
        var plain = Synthesizer.Tone(440, 1, Waveform.Square, Rate);
        var sustained = Synthesizer.Tone(440, 1, Waveform.Square, Rate, Envelope.Sustained);
        Assert.Equal(plain.Samples.ToArray(), sustained.Samples.ToArray());

        // flat in the middle, faded at both ends
        Assert.Equal(1.0, Peak(plain.Samples.Slice(Rate / 2 - 100, 200)));
        Assert.Equal(0.0, plain.Samples[0]);
        Assert.Equal(0.0, Peak(plain.Samples.Slice(plain.Length - 1, 1)));
    }

    [Fact]
    public void Plucked_tone_decays()
    {
        var track = Synthesizer.Tone(440, 2, Waveform.Square, Rate, Envelope.Plucked);
        var early = Peak(track.Samples.Slice(Rate / 20, 1000));       // ~50 ms in
        var late = Peak(track.Samples.Slice(Rate * 3 / 2, 1000));      // ~1.5 s in
        Assert.True(early > 0.85, $"early {early}");
        Assert.True(late < 0.1, $"late {late}");
    }

    [Fact]
    public void Envelope_gain_stays_within_unit_range_even_for_very_short_notes()
    {
        foreach (var envelope in new[] { Envelope.Sustained, Envelope.Plucked })
        {
            for (var count = 1; count < 50; count++)
                for (var i = 0; i < count; i++)
                    Assert.InRange(envelope.Gain(i, count, Rate), 0.0, 1.0);
        }
    }

    [Fact]
    public void Envelope_is_passed_through_chords()
    {
        var sustained = Synthesizer.Chord(Chord.Major(Pitch.C4), 2, Waveform.Sine, Rate);
        var plucked = Synthesizer.Chord(Chord.Major(Pitch.C4), 2, Waveform.Sine, Rate, Envelope.Plucked);
        Assert.True(Peak(plucked.Samples.Slice(Rate * 3 / 2, 1000)) < Peak(sustained.Samples.Slice(Rate * 3 / 2, 1000)) / 4);
    }
}
