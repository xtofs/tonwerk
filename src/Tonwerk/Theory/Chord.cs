namespace Tonwerk.Theory;

/// <summary>A set of pitches sounding together.</summary>
public readonly struct Chord(params Pitch[] pitches)
{
    /// <summary>The pitches of the chord (empty for the default instance).</summary>
    public ReadOnlySpan<Pitch> Pitches => pitches;

    /// <summary>Major triad, e.g. C E G.</summary>
    public static Chord Major(Pitch root) => new(root, root + Interval.M3, root + Interval.P5);

    /// <summary>Minor triad, e.g. C Eb G.</summary>
    public static Chord Minor(Pitch root) => new(root, root + Interval.m3, root + Interval.P5);

    /// <summary>Diminished triad, e.g. B° = B D F.</summary>
    public static Chord Diminished(Pitch root) => new(root, root + Interval.m3, root + Interval.TT);

    /// <summary>Dominant seventh, e.g. G7 = G B D F (the chord on the 5th degree plus a minor seventh).</summary>
    public static Chord DominantSeventh(Pitch root) => new(root, root + Interval.M3, root + Interval.P5, root + Interval.m7);

    /// <summary>Minor seventh, e.g. Dm7 = D F A C.</summary>
    public static Chord MinorSeventh(Pitch root) => new(root, root + Interval.m3, root + Interval.P5, root + Interval.m7);

    public override string ToString() => pitches is null ? "" : string.Join(" ", pitches);
}
