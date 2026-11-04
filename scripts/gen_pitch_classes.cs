#:project ../src/Tonwerk/Tonwerk.csproj

// Generates the named PitchClass constants (paste the output into the marked
// section of src/Tonwerk/Theory/PitchClass.cs):
//
//     dotnet run scripts/gen_pitch_classes.cs
//
// It uses only the public API: PitchClass.All, Offset, Semitone and ToString().

var all = PitchClass.All.ToArray();

foreach (var pc in all)
{
    // a legal C# identifier: F# -> FSharp, Bb -> BFlat, Cx -> CDoubleSharp
    var name = pc.ToString().Replace("#", "Sharp").Replace("b", "Flat").Replace("x", "DoubleSharp");

    // enharmonic synonyms: other spellings of the same semitone
    var synonyms = all.Where(other => other.Semitone == pc.Semitone && other.Offset != pc.Offset).ToArray();
    var comment = synonyms.Length > 0 ? " synonym " + string.Join(", ", synonyms) : "";

    Console.WriteLine($"public static readonly PitchClass {name,-6} = new PitchClass({pc.Offset,2}); // {pc,-2} semitone {pc.Semitone,2}{comment}");
}
