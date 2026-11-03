
internal class Program
{

    static readonly char[] DiatonicLetter = "CDEFGAB".ToCharArray();

    static readonly Dictionary<int, string> Accidentals = new Dictionary<int, string> { { -2, "bb" }, { -1, "b" }, { 0, "" }, { 1, "#" }, { 2, "x" } };

    private static int Mod(int a, int n) => ((a % n) + n) % n;
    private static int FloorDiv(int a, int n) => (int)Math.Floor((double)a / n);

    private static void Main(string[] args)
    {

        var pcs = Enumerable
            .Range(-8, 21)
            .Select(k => new PitchClass(k));

        foreach (var p in pcs)
        {
            var synonyms = pcs.Where(pc => pc.Semitone == p.Semitone).Where(pc => pc.Offset != p.Offset).ToArray();
            var name = p.ToString().Replace("#", "Sharp").Replace("b", "Flat").Replace("x", "DoubleSharp");
            var syn = synonyms.Length > 0 ? "synonym " + string.Join(", ", synonyms) : "";
            Console.WriteLine($"public static readonly PitchClass {name,-6} = new PitchClass({p.Offset,2}); // {p,-2} semitone {p.Semitone,2} {syn}");
        }
    }
}
