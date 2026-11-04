namespace Tonwerk.Theory;

public enum Mode
{
    /// <summary>Ionian: the major scale, 2 2 1 2 2 2 1 semitones.</summary>
    Major,
    /// <summary>Natural minor (Aeolian): 2 1 2 2 1 2 2.</summary>
    Minor,
    /// <summary>
    /// Dorian: natural minor with a raised sixth degree, 2 1 2 2 2 1 2 — a minor sound
    /// with a major IV chord (e.g. Scarborough Fair, "So What").
    /// </summary>
    Dorian,
}

/// <summary>
/// A musical key: a tonic pitch plus a mode. The key maps scale degrees
/// (1-7, as used by roman numerals) to pitches.
/// </summary>
public readonly record struct Key(Pitch Tonic, Mode Mode)
{
    public static Key Major(Pitch tonic) => new(tonic, Mode.Major);
    public static Key Minor(Pitch tonic) => new(tonic, Mode.Minor);
    public static Key Dorian(Pitch tonic) => new(tonic, Mode.Dorian);

    // semitone offsets of the scale degrees from the tonic
    private static readonly int[] MajorOffsets = [0, 2, 4, 5, 7, 9, 11];
    private static readonly int[] NaturalMinorOffsets = [0, 2, 3, 5, 7, 8, 10];
    private static readonly int[] DorianOffsets = [0, 2, 3, 5, 7, 9, 10];

    /// <summary>
    /// The root pitch of the given scale degree (1-7), voiced closest to the
    /// tonic: offsets beyond a tritone are dropped an octave (so in C major,
    /// degree 5 is the G below the tonic, not above).
    /// </summary>
    public Pitch Root(int degree)
    {
        if (degree is < 1 or > 7)
        {
            throw new ArgumentOutOfRangeException(nameof(degree), degree, "Scale degree must be 1-7");
        }

        var offsets = Mode switch
        {
            Mode.Major => MajorOffsets,
            Mode.Minor => NaturalMinorOffsets,
            Mode.Dorian => DorianOffsets,
            _ => throw new InvalidOperationException($"Unknown mode: {Mode}")
        };
        var offset = offsets[degree - 1];
        if (offset > 6) offset -= 12; // choose the root closest to the tonic
        return Tonic + offset;
    }

    public override string ToString() => $"{Tonic} {Mode.ToString().ToLowerInvariant()}";
}
