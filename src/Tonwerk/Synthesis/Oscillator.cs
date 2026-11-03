namespace Tonwerk.Synthesis;

/// <summary>One-cycle shapes for the <see cref="Waveform"/>s.</summary>
public static class Oscillator
{
    /// <summary>
    /// The value of the waveform at the given phase.
    /// </summary>
    /// <param name="waveform">The oscillator shape.</param>
    /// <param name="phase">Position within one cycle, in [0, 1).</param>
    /// <returns>The amplitude, in [-1, 1].</returns>
    public static double Sample(Waveform waveform, double phase) => waveform switch
    {
        Waveform.Sine => Math.Sin(2 * Math.PI * phase),
        Waveform.Square => phase < 0.5 ? 1.0 : -1.0,
        Waveform.Sawtooth => 2 * phase - 1,
        // rises 0 -> 1, falls 1 -> -1, rises -1 -> 0 (same zero crossings as sine)
        Waveform.Triangle => phase < 0.25 ? 4 * phase
                           : phase < 0.75 ? 2 - 4 * phase
                           : 4 * phase - 4,
        Waveform.Gaussian => Math.Exp(-Math.Pow(phase - 0.5, 2) * 4),
        _ => throw new ArgumentOutOfRangeException(nameof(waveform), waveform, null)
    };
}
