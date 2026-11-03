namespace Tonwerk.Theory;

/// <summary>
/// Parses roman-numeral harmony notation into chords, relative to a key.
/// Example: <c>Harmony.Parse(Key.Major(Pitch.C4), "I V7 I V7")</c>.
///
/// Each symbol is a <see cref="RomanNumeral"/> (see its parsing rules) resolved
/// against the key. Bar lines (<c>|</c>) are ignored, like in the melody notation.
/// </summary>
public static class Harmony
{
    public static Chord[] Parse(Key key, string text)
    {
        var chords = new List<Chord>();
        var span = text.AsSpan();

        while (true)
        {
            span = MonophonicMelody.SkipSeparators(span);
            if (span.IsEmpty) break;

            if (!RomanNumeral.TryParse(span, out var numeral, out var rest))
            {
                throw new FormatException($"Invalid roman numeral at: \"{MonophonicMelody.ErrorSnippet(span)}\"");
            }
            chords.Add(numeral.ToChord(key));
            span = rest;
        }
        return [.. chords];
    }

    /// <summary>Parses a harmony like "I V7 I V7" relative to the given key.</summary>
    public static Chord[] ParseHarmony(this string text, Key key) => Parse(key, text);
}
