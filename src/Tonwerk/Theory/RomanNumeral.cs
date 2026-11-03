namespace Tonwerk.Theory;

public enum ChordQuality
{
    Major,
    Minor,
    Dominant7,
    Minor7,
    Diminished,
}

/// <summary>
/// A roman-numeral chord symbol — a scale degree (1-7) plus a chord quality —
/// independent of any key: "V7" is (5, Dominant7), "ii" is (2, Minor).
/// Resolve it against a key with <see cref="ToChord"/>.
/// </summary>
public readonly record struct RomanNumeral(int Degree, ChordQuality Quality) : IPrefixParsable<RomanNumeral>
{
    public override string ToString()
    {
        var numeral = Numerals[Degree - 1];
        return Quality switch
        {
            ChordQuality.Major => numeral,
            ChordQuality.Dominant7 => numeral + "7",
            ChordQuality.Minor => numeral.ToLowerInvariant(),
            ChordQuality.Minor7 => numeral.ToLowerInvariant() + "7",
            ChordQuality.Diminished => numeral.ToLowerInvariant() + "°",
            _ => throw new InvalidOperationException($"Unknown chord quality: {Quality}")
        };
    }

    /// <summary>Resolves the numeral to a chord in the given key.</summary>
    public Chord ToChord(Key key)
    {
        var root = key.Root(Degree);
        return Quality switch
        {
            ChordQuality.Major => Chord.Major(root),
            ChordQuality.Minor => Chord.Minor(root),
            ChordQuality.Dominant7 => Chord.DominantSeventh(root),
            ChordQuality.Minor7 => Chord.MinorSeventh(root),
            ChordQuality.Diminished => Chord.Diminished(root),
            _ => throw new InvalidOperationException($"Unknown chord quality: {Quality}")
        };
    }

    /// <summary>
    /// Prefix-parses a roman-numeral chord symbol: numeral letters (their case
    /// gives the quality: uppercase = major, lowercase = minor), optionally
    /// followed by <c>7</c> (a seventh: dominant on uppercase, minor on
    /// lowercase) or <c>°</c> / ASCII <c>o</c> (diminished, lowercase only).
    /// </summary>
    public static bool TryParse(ReadOnlySpan<char> s, out RomanNumeral result, out ReadOnlySpan<char> remainder)
    {
        result = default;
        remainder = s;

        // the leading run of numeral letters
        var length = s.IndexOfAnyExcept("IViv");
        if (length < 0) length = s.Length;
        if (length == 0) return false;
        var numeral = s[..length];

        var isUpper = char.IsUpper(numeral[0]);
        foreach (var c in numeral)
        {
            if (char.IsUpper(c) != isUpper) return false; // mixed case has no defined quality
        }

        var degree = DegreeOf(numeral);
        if (degree == 0) return false;

        s = s[length..];
        ChordQuality quality;
        if (!s.IsEmpty && s[0] == '7')
        {
            quality = isUpper ? ChordQuality.Dominant7 : ChordQuality.Minor7;
            s = s[1..];
        }
        else if (!s.IsEmpty && s[0] is '°' or 'o')
        {
            if (isUpper) return false; // diminished numerals are written lowercase
            quality = ChordQuality.Diminished;
            s = s[1..];
        }
        else
        {
            quality = isUpper ? ChordQuality.Major : ChordQuality.Minor;
        }

        result = new RomanNumeral(degree, quality);
        remainder = s;
        return true;
    }

    private static readonly string[] Numerals = ["I", "II", "III", "IV", "V", "VI", "VII"];

    private static int DegreeOf(ReadOnlySpan<char> numeral)
    {
        Span<char> upper = stackalloc char[numeral.Length];
        numeral.ToUpperInvariant(upper);
        return upper switch
        {
            "I" => 1,
            "II" => 2,
            "III" => 3,
            "IV" => 4,
            "V" => 5,
            "VI" => 6,
            "VII" => 7,
            _ => 0
        };
    }
}
