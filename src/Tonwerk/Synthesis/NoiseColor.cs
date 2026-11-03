namespace Tonwerk.Synthesis;

/// <summary>The classic "colors" of noise, named by their power spectrum (power ∝ 1/f^α).</summary>
public enum NoiseColor
{
    /// <summary>Flat spectrum (α = 0).</summary>
    White,
    /// <summary>−3 dB/octave (α = 1).</summary>
    Pink,
    /// <summary>−6 dB/octave (α = 2), also called red noise.</summary>
    Brown,
    /// <summary>+3 dB/octave (α = −1).</summary>
    Blue,
    /// <summary>+6 dB/octave (α = −2).</summary>
    Violet,
}
