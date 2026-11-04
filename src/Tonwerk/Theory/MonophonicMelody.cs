namespace Tonwerk.Theory;


/// <summary>
/// Provides utility methods for working with monophonic melodies, represented as sequences of notes.
/// </summary>
public static class MonophonicMelody
{
    /// <summary>
    /// Parses a melody like "E4/4 E F G | ..." into notes (pitch + ticks,
    /// 960 ticks per quarter note). Octave and duration are sticky: omitted
    /// values repeat the previous note's, so only changes need to be written.
    /// The first note must specify both.
    /// </summary>
    public static Note[] ParseMelody(this string s)
    {
        var notes = new List<Note>();
        var span = s.AsSpan();
        Note? previous = null;

        while (true)
        {
            span = SkipSeparators(span);
            if (span.IsEmpty) break;

            if (!Note.TryParse(span, previous, out var note, out var rest))
            {
                throw new FormatException($"Invalid note at: \"{ErrorSnippet(span)}\"");
            }
            notes.Add(note);
            previous = note;
            span = rest;
        }
        return [.. notes];
    }

    // whitespace and bar lines separate notes; bar lines are purely visual
    internal static ReadOnlySpan<char> SkipSeparators(ReadOnlySpan<char> s)
    {
        var skip = 0;
        while (skip < s.Length && (char.IsWhiteSpace(s[skip]) || s[skip] == '|')) skip++;
        return s[skip..];
    }

    internal static string ErrorSnippet(ReadOnlySpan<char> s)
    {
        var end = s.IndexOfAny(" \t\r\n|");
        if (end < 0) end = s.Length;
        return s[..Math.Min(end, 16)].ToString();
    }
}

