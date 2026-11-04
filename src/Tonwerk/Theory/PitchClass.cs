namespace Tonwerk.Theory;

using System.Collections.Frozen;
using System.Diagnostics.CodeAnalysis;

public readonly record struct PitchClass : IPrefixParsable<PitchClass>
{

    internal PitchClass(int index) => this.Offset = index;

    /// <summary>
    /// represented by the number of fifth from C.
    /// leading to the sequence Bbb -9, Fb -8, Cb -7, ... Bb -2, F -1, C 0, G 1, D 2, A 3, ... A# 10, E# 11
    /// </summary>
    public int Offset { get; }

    private static readonly string DiatonicLetter = "CDEFGAB";

    static readonly IDictionary<int, string> Accidentals = new Dictionary<int, string> { { -2, "bb" }, { -1, "b" }, { 0, "" }, { 1, "#" }, { 2, "x" } }.ToFrozenDictionary();

    public char Letter => DiatonicLetter[Mod(4 * Offset, 7)];

    public int Semitone => Mod(7 * Offset, 12);

    public string Accidental => Accidentals[FloorDiv(Offset + 1, 7)];

    public override string ToString()
    {
        return $"{Letter}{Accidental}";
    }

    public static bool TryParse(ReadOnlySpan<char> s, [MaybeNullWhen(returnValue: false)] out PitchClass result, out ReadOnlySpan<char> remainder)
    {
        result = default;
        remainder = s;
        if (s.IsEmpty)
        {
            return false;
        }
        int letterIndex = DiatonicLetter.IndexOf(s[0]);
        if (letterIndex == -1)
        {
            return false;
        }
        s = s[1..];
        int accidental = 0;
        if (!s.IsEmpty)
        {
            if (s.StartsWith("bb"))
            {
                accidental = -2;
                s = s[2..];
            }
            else if (s[0] == 'b')
            {
                accidental = -1;
                s = s[1..];
            }
            else if (s[0] == '#')
            {
                accidental = 1;
                s = s[1..];
            }
            else if (s[0] == 'x')
            {
                accidental = 2;
                s = s[1..];
            }
        }
        // line-of-fifths offset of the natural letter (F -1, C 0, G 1, D 2, A 3, E 4, B 5):
        // the inverse of Letter = DiatonicLetter[Mod(4 * Offset, 7)]
        int naturalOffset = Mod(2 * letterIndex + 1, 7) - 1;
        result = new PitchClass(naturalOffset + accidental * 7);
        remainder = s;
        return true;
    }

    // the named constants below span Fb (-8) to B# (12)
    private const int FirstOffset = -8;
    private const int LastOffset = 12;

    /// <summary>
    /// All named pitch classes in line-of-fifths order, from Fb (offset −8) to B# (offset 12):
    /// each element is a perfect fifth above the previous one.
    /// </summary>
    public static IEnumerable<PitchClass> All =>
        Enumerable.Range(FirstOffset, LastOffset - FirstOffset + 1).Select(offset => new PitchClass(offset));

    private static int Mod(int a, int n) => ((a % n) + n) % n;
    private static int FloorDiv(int a, int n) => (int)Math.Floor((double)a / n);




    public static readonly PitchClass FFlat = new PitchClass(-8); // Fb semitone  4 synonym E
    public static readonly PitchClass CFlat = new PitchClass(-7); // Cb semitone 11 synonym B
    public static readonly PitchClass GFlat = new PitchClass(-6); // Gb semitone  6 synonym F#
    public static readonly PitchClass DFlat = new PitchClass(-5); // Db semitone  1 synonym C#
    public static readonly PitchClass AFlat = new PitchClass(-4); // Ab semitone  8 synonym G#
    public static readonly PitchClass EFlat = new PitchClass(-3); // Eb semitone  3 synonym D#
    public static readonly PitchClass BFlat = new PitchClass(-2); // Bb semitone 10 synonym A#
    public static readonly PitchClass F = new PitchClass(-1); // F  semitone  5 synonym E#
    public static readonly PitchClass C = new PitchClass(0); // C  semitone  0 synonym B#
    public static readonly PitchClass G = new PitchClass(1); // G  semitone  7 
    public static readonly PitchClass D = new PitchClass(2); // D  semitone  2 
    public static readonly PitchClass A = new PitchClass(3); // A  semitone  9 
    public static readonly PitchClass E = new PitchClass(4); // E  semitone  4 synonym Fb
    public static readonly PitchClass B = new PitchClass(5); // B  semitone 11 synonym Cb
    public static readonly PitchClass FSharp = new PitchClass(6); // F# semitone  6 synonym Gb
    public static readonly PitchClass CSharp = new PitchClass(7); // C# semitone  1 synonym Db
    public static readonly PitchClass GSharp = new PitchClass(8); // G# semitone  8 synonym Ab
    public static readonly PitchClass DSharp = new PitchClass(9); // D# semitone  3 synonym Eb
    public static readonly PitchClass ASharp = new PitchClass(10); // A# semitone 10 synonym Bb
    public static readonly PitchClass ESharp = new PitchClass(11); // E# semitone  5 synonym F
    public static readonly PitchClass BSharp = new PitchClass(12); // B# semitone  0 synonym C

}





