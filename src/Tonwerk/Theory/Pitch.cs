namespace Tonwerk.Theory;

/// <summary>
/// A pitch in twelve-tone equal temperament, measured in semitones relative to A4 = 0.
/// Arithmetic is affine: a pitch can be transposed by an integer number of semitones,
/// but two pitches cannot be added.
/// </summary>
public readonly record struct Pitch(int Semitone) : IPrefixParsable<Pitch>
{
    /// <summary>Reference frequency of A4 in Hz.</summary>
    public const double A4Frequency = 440.0;

    /// <summary>Frequency in Hz (equal temperament, A4 = 440 Hz).</summary>
    public double Frequency => A4Frequency * Math.Pow(2, Semitone / 12.0);

    /// <summary>Octave number in scientific pitch notation (A4 is in octave 4).</summary>
    public int Octave => (FromC0 - PitchClass) / 12;

    /// <summary>Position within the octave: C = 0 ... B = 11.</summary>
    public int PitchClass => ((FromC0 % 12) + 12) % 12;

    // Semitone is relative to A4 = 0, so C0 is -57.
    private int FromC0 => Semitone + 57;

    // transpose by semitones
    public static Pitch operator +(Pitch pitch, int semitones) => new(pitch.Semitone + semitones);
    public static Pitch operator -(Pitch pitch, int semitones) => new(pitch.Semitone - semitones);

    public override string ToString() => $"{PitchNames[PitchClass]}{Octave}";

    public static Pitch Parse(string s, IFormatProvider? provider = null) =>
        TryParse(s, provider, out var result) ? result : throw new FormatException($"Invalid pitch format: {s}");

    public static bool TryParse(string? str, IFormatProvider? provider, out Pitch result) =>
        TryParse(str.AsSpan(), out result);

    /// <summary>
    /// Parses a pitch like "E4", "F#3" or "Bb2": a note letter (A-G),
    /// an optional sharp (#) or flat (b), and an octave digit (0-8).
    /// </summary>
    public static bool TryParse(ReadOnlySpan<char> span, out Pitch result) =>
        TryParse(span, defaultOctave: -1, out result);

    /// <summary>
    /// Parses a pitch whose octave digit may be omitted; an omitted octave
    /// falls back to <paramref name="defaultOctave"/> (pass -1 to require one).
    /// </summary>
    public static bool TryParse(ReadOnlySpan<char> span, int defaultOctave, out Pitch result)
    {
        if (TryParse(span, defaultOctave, out result, out var remainder) && remainder.IsEmpty) return true;
        result = default;
        return false;
    }

    /// <summary>
    /// Prefix-parses a pitch (note letter, optional accidental, octave digit)
    /// from the start of the span; <paramref name="remainder"/> receives the
    /// unparsed rest.
    /// </summary>
    public static bool TryParse(ReadOnlySpan<char> s, out Pitch result, out ReadOnlySpan<char> remainder) =>
        TryParse(s, defaultOctave: -1, out result, out remainder);

    /// <summary>
    /// Prefix-parses a pitch whose octave digit may be omitted (falling back to
    /// <paramref name="defaultOctave"/>; pass -1 to require one). All other
    /// TryParse overloads delegate here.
    /// </summary>
    public static bool TryParse(ReadOnlySpan<char> s, int defaultOctave, out Pitch result, out ReadOnlySpan<char> remainder)
    {
        result = default;
        remainder = s;
        if (s.IsEmpty || !PitchOffset.TryGetValue(s[0], out var pitch)) return false;
        s = s[1..]; // remove the note character

        if (!s.IsEmpty && s[0] is '#' or 'b')
        {
            pitch += s[0] == '#' ? 1 : -1;
            s = s[1..]; // remove the accidental
        }

        int octave;
        if (!s.IsEmpty && char.IsAsciiDigit(s[0]))
        {
            octave = s[0] - '0';
            s = s[1..]; // remove the octave digit
        }
        else if (defaultOctave >= 0)
        {
            octave = defaultOctave;
        }
        else
        {
            return false; // octave required, none given
        }

        result = new Pitch(-57 + pitch + (octave * 12));
        remainder = s;
        return true;
    }

    // note offset within the octave (C=0, D=2, E=4, F=5, G=7, A=9, B=11)
    private static readonly Dictionary<char, int> PitchOffset = new() { ['C'] = 0, ['D'] = 2, ['E'] = 4, ['F'] = 5, ['G'] = 7, ['A'] = 9, ['B'] = 11 };

    private static readonly string[] PitchNames = ["C", "C#", "D", "D#", "E", "F", "F#", "G", "G#", "A", "A#", "B"];

    // builds a constant from pitch class (C=0 ... B=11) and octave
    private static Pitch FromPitchClassAndOctave(int pitchClass, int octave) => new(-57 + pitchClass + (octave * 12));

    // named pitches for the common range, octaves 2-6
    // (sharps as Cs, flats as aliases: Df == Cs)

    public static readonly Pitch C2 = FromPitchClassAndOctave(0, 2);
    public static readonly Pitch Cs2 = FromPitchClassAndOctave(1, 2);
    public static readonly Pitch Df2 = Cs2;
    public static readonly Pitch D2 = FromPitchClassAndOctave(2, 2);
    public static readonly Pitch Ds2 = FromPitchClassAndOctave(3, 2);
    public static readonly Pitch Ef2 = Ds2;
    public static readonly Pitch E2 = FromPitchClassAndOctave(4, 2);
    public static readonly Pitch F2 = FromPitchClassAndOctave(5, 2);
    public static readonly Pitch Fs2 = FromPitchClassAndOctave(6, 2);
    public static readonly Pitch Gf2 = Fs2;
    public static readonly Pitch G2 = FromPitchClassAndOctave(7, 2);
    public static readonly Pitch Gs2 = FromPitchClassAndOctave(8, 2);
    public static readonly Pitch Af2 = Gs2;
    public static readonly Pitch A2 = FromPitchClassAndOctave(9, 2);
    public static readonly Pitch As2 = FromPitchClassAndOctave(10, 2);
    public static readonly Pitch Bf2 = As2;
    public static readonly Pitch B2 = FromPitchClassAndOctave(11, 2);

    public static readonly Pitch C3 = FromPitchClassAndOctave(0, 3);
    public static readonly Pitch Cs3 = FromPitchClassAndOctave(1, 3);
    public static readonly Pitch Df3 = Cs3;
    public static readonly Pitch D3 = FromPitchClassAndOctave(2, 3);
    public static readonly Pitch Ds3 = FromPitchClassAndOctave(3, 3);
    public static readonly Pitch Ef3 = Ds3;
    public static readonly Pitch E3 = FromPitchClassAndOctave(4, 3);
    public static readonly Pitch F3 = FromPitchClassAndOctave(5, 3);
    public static readonly Pitch Fs3 = FromPitchClassAndOctave(6, 3);
    public static readonly Pitch Gf3 = Fs3;
    public static readonly Pitch G3 = FromPitchClassAndOctave(7, 3);
    public static readonly Pitch Gs3 = FromPitchClassAndOctave(8, 3);
    public static readonly Pitch Af3 = Gs3;
    public static readonly Pitch A3 = FromPitchClassAndOctave(9, 3);
    public static readonly Pitch As3 = FromPitchClassAndOctave(10, 3);
    public static readonly Pitch Bf3 = As3;
    public static readonly Pitch B3 = FromPitchClassAndOctave(11, 3);

    public static readonly Pitch C4 = FromPitchClassAndOctave(0, 4);
    public static readonly Pitch Cs4 = FromPitchClassAndOctave(1, 4);
    public static readonly Pitch Df4 = Cs4;
    public static readonly Pitch D4 = FromPitchClassAndOctave(2, 4);
    public static readonly Pitch Ds4 = FromPitchClassAndOctave(3, 4);
    public static readonly Pitch Ef4 = Ds4;
    public static readonly Pitch E4 = FromPitchClassAndOctave(4, 4);
    public static readonly Pitch F4 = FromPitchClassAndOctave(5, 4);
    public static readonly Pitch Fs4 = FromPitchClassAndOctave(6, 4);
    public static readonly Pitch Gf4 = Fs4;
    public static readonly Pitch G4 = FromPitchClassAndOctave(7, 4);
    public static readonly Pitch Gs4 = FromPitchClassAndOctave(8, 4);
    public static readonly Pitch Af4 = Gs4;
    public static readonly Pitch A4 = FromPitchClassAndOctave(9, 4);
    public static readonly Pitch As4 = FromPitchClassAndOctave(10, 4);
    public static readonly Pitch Bf4 = As4;
    public static readonly Pitch B4 = FromPitchClassAndOctave(11, 4);

    public static readonly Pitch C5 = FromPitchClassAndOctave(0, 5);
    public static readonly Pitch Cs5 = FromPitchClassAndOctave(1, 5);
    public static readonly Pitch Df5 = Cs5;
    public static readonly Pitch D5 = FromPitchClassAndOctave(2, 5);
    public static readonly Pitch Ds5 = FromPitchClassAndOctave(3, 5);
    public static readonly Pitch Ef5 = Ds5;
    public static readonly Pitch E5 = FromPitchClassAndOctave(4, 5);
    public static readonly Pitch F5 = FromPitchClassAndOctave(5, 5);
    public static readonly Pitch Fs5 = FromPitchClassAndOctave(6, 5);
    public static readonly Pitch Gf5 = Fs5;
    public static readonly Pitch G5 = FromPitchClassAndOctave(7, 5);
    public static readonly Pitch Gs5 = FromPitchClassAndOctave(8, 5);
    public static readonly Pitch Af5 = Gs5;
    public static readonly Pitch A5 = FromPitchClassAndOctave(9, 5);
    public static readonly Pitch As5 = FromPitchClassAndOctave(10, 5);
    public static readonly Pitch Bf5 = As5;
    public static readonly Pitch B5 = FromPitchClassAndOctave(11, 5);

    public static readonly Pitch C6 = FromPitchClassAndOctave(0, 6);
    public static readonly Pitch Cs6 = FromPitchClassAndOctave(1, 6);
    public static readonly Pitch Df6 = Cs6;
    public static readonly Pitch D6 = FromPitchClassAndOctave(2, 6);
    public static readonly Pitch Ds6 = FromPitchClassAndOctave(3, 6);
    public static readonly Pitch Ef6 = Ds6;
    public static readonly Pitch E6 = FromPitchClassAndOctave(4, 6);
    public static readonly Pitch F6 = FromPitchClassAndOctave(5, 6);
    public static readonly Pitch Fs6 = FromPitchClassAndOctave(6, 6);
    public static readonly Pitch Gf6 = Fs6;
    public static readonly Pitch G6 = FromPitchClassAndOctave(7, 6);
    public static readonly Pitch Gs6 = FromPitchClassAndOctave(8, 6);
    public static readonly Pitch Af6 = Gs6;
    public static readonly Pitch A6 = FromPitchClassAndOctave(9, 6);
    public static readonly Pitch As6 = FromPitchClassAndOctave(10, 6);
    public static readonly Pitch Bf6 = As6;
    public static readonly Pitch B6 = FromPitchClassAndOctave(11, 6);
}
