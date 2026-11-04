namespace Tonwerk.Synthesis;

/// <summary>
/// The loudness of a tone over its lifetime: a linear attack, an exponential decay
/// while the note is held, and a linear release at the end of the note.
/// A flat envelope (<see cref="Sustained"/>) sounds like an organ; a decaying one
/// (<see cref="Plucked"/>) like a plucked or struck instrument.
/// </summary>
/// <param name="AttackSeconds">Linear fade-in from silence to full level.</param>
/// <param name="DecaySeconds">
/// Time constant of the exponential decay: after this long the level has dropped
/// to 1/e (37%). <see cref="double.PositiveInfinity"/> means no decay.
/// </param>
/// <param name="ReleaseSeconds">Linear fade-out at the end of the note (also prevents a click).</param>
public readonly record struct Envelope(double AttackSeconds, double DecaySeconds, double ReleaseSeconds)
{
    /// <summary>Constant level with a 10 ms fade in/out that only avoids clicks (organ-like).</summary>
    public static Envelope Sustained { get; } = new(0.01, double.PositiveInfinity, 0.01);

    /// <summary>Quick attack, then a decay that dies away within about a second (plucked/struck-like).</summary>
    public static Envelope Plucked { get; } = new(0.005, 0.5, 0.05);

    /// <summary>A plucked envelope with the given decay time constant.</summary>
    public static Envelope Decaying(double decaySeconds) => new(0.005, decaySeconds, 0.05);

    /// <summary>The gain in [0, 1] at sample <paramref name="index"/> of a note of <paramref name="count"/> samples.</summary>
    public double Gain(int index, int count, int rate)
    {
        // never longer than half the note, never shorter than one sample
        var attack = Math.Max(1, Math.Min((int)(AttackSeconds * rate), count / 2));
        var release = Math.Max(1, Math.Min((int)(ReleaseSeconds * rate), count / 2));

        var rise = Math.Min(1.0, index / (double)attack);
        var fall = Math.Min(1.0, (count - 1 - index) / (double)release);
        var decay = Math.Exp(-(index / (double)rate) / DecaySeconds); // 1 when DecaySeconds is infinite
        return Math.Min(rise, fall) * decay;
    }
}
