namespace Tonwerk.Theory;

/// <summary>
/// A note in a melody is a <see cref="Pitch"/> plus a duration measured in
/// integer ticks, with 960 ticks per quarter note (960 PPQN). The fixed-point
/// resolution 960 = 2^6 * 3 * 5 keeps dotted notes, triplets and quintuplets exact.
/// </summary>
public readonly record struct Note(Pitch Pitch, int Ticks) : IPrefixParsable<Note>
{
    public const int TicksPerQuarter = 960;
    public const int TicksPerWhole = 4 * TicksPerQuarter;

    /// <summary>Duration in floating point beats, where a quarter note is one beat.</summary>
    public double Beats => Ticks / (double)TicksPerQuarter;

    /// <summary>Duration in seconds at the given tempo.</summary>
    public double ToSeconds(double beatsPerMinute) => Beats * 60.0 / beatsPerMinute;

    public override string ToString()
    {
        // find the note value (plain or dotted) that matches the tick count exactly
        for (var denominator = 1; denominator <= 64; denominator *= 2)
        {
            var plain = TicksPerWhole / denominator;
            if (Ticks == plain) return $"{Pitch}/{denominator}";
            if (Ticks == plain + plain / 2) return $"{Pitch}/{denominator}.";
        }
        return $"{Pitch}/{Ticks}t"; // not notatable as a (dotted) power-of-two value
    }

    public static Note Parse(string s, IFormatProvider? provider) => TryParse(s, provider, out var result) ? result : throw new FormatException($"Invalid note format: {s}");

    public static bool TryParse(string? str, IFormatProvider? provider, out Note result) =>
        TryParse(str.AsSpan(), previous: null, out result);

    /// <summary>
    /// Prefix-parses a note (pitch with explicit octave, followed by /duration)
    /// from the start of the span; <paramref name="remainder"/> receives the
    /// unparsed rest.
    /// </summary>
    public static bool TryParse(ReadOnlySpan<char> s, out Note result, out ReadOnlySpan<char> remainder) =>
        TryParse(s, previous: null, out result, out remainder);

    /// <summary>
    /// Parses a note like "E4/4" into a pitch and duration. The octave and the
    /// duration are "sticky": either may be omitted, in which case the value of
    /// <paramref name="previous"/> is used — so a melody can be written as
    /// "E4/4 E F G". The first note of a melody (previous = null) must specify both.
    /// </summary>
    public static bool TryParse(ReadOnlySpan<char> span, Note? previous, out Note result)
    {
        if (TryParse(span, previous, out result, out var remainder) && remainder.IsEmpty) return true;
        result = default;
        return false;
    }

    /// <summary>
    /// Prefix-parses a (sticky) note from the start of the span; the pitch parser
    /// reports how much it consumed, and the optional /duration continues from
    /// there. All other TryParse overloads delegate here.
    /// </summary>
    public static bool TryParse(ReadOnlySpan<char> s, Note? previous, out Note result, out ReadOnlySpan<char> remainder)
    {
        result = default;
        remainder = s;

        var defaultOctave = previous?.Pitch.Octave ?? -1;
        if (!Pitch.TryParse(s, defaultOctave, out var pitch, out var rest)) return false;

        int ticks;
        if (!rest.IsEmpty && rest[0] == '/')
        {
            if (!TryParseDuration(rest[1..], out ticks, out rest)) return false;
        }
        else if (previous is { } previousNote)
        {
            ticks = previousNote.Ticks; // sticky duration
        }
        else
        {
            return false; // the first note must have a duration
        }

        result = new Note(pitch, ticks);
        remainder = rest;
        return true;
    }

    /// <summary>
    /// Prefix-parses a duration: a (possibly multi-digit) note value denominator
    /// such as 4 or 16, optionally followed by a dot (x1.5), into ticks.
    /// </summary>
    private static bool TryParseDuration(ReadOnlySpan<char> s, out int ticks, out ReadOnlySpan<char> remainder)
    {
        ticks = 0;
        remainder = s;

        var denominator = 0;
        while (!s.IsEmpty && char.IsAsciiDigit(s[0]))
        {
            denominator = denominator * 10 + (s[0] - '0');
            s = s[1..];
        }
        if (denominator == 0 || TicksPerWhole % denominator != 0) return false;

        ticks = TicksPerWhole / denominator;

        if (!s.IsEmpty && s[0] == '.')
        {
            if (ticks % 2 != 0) return false; // dotted value must stay an integer
            ticks += ticks / 2;
            s = s[1..];
        }

        remainder = s;
        return true;
    }

    internal (double Frequency, double Duration) FrequencyAndDuration(double bpm)
    {
        var frequency = this.Pitch.Frequency;
        var duration = this.ToSeconds(bpm);
        return (frequency, duration);
    }
}


