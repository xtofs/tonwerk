namespace Tonwerk.Tests;

public class PitchClassTests
{
    /// <summary>
    /// Every spelling ToString can produce: 7 letters x 5 accidentals
    /// ("" natural, "#" sharp, "b" flat, "bb" double flat, "x" double sharp).
    /// </summary>
    public static TheoryData<string> AllSpellings()
    {
        var data = new TheoryData<string>();
        foreach (var letter in "CDEFGAB")
        {
            foreach (var accidental in new[] { "", "#", "b", "bb", "x" })
            {
                data.Add($"{letter}{accidental}");
            }
        }
        return data;
    }

    [Theory]
    [MemberData(nameof(AllSpellings))]
    public void TryParse_ToString_RoundTrips(string spelling)
    {
        Assert.True(PitchClass.TryParse(spelling, out var pitchClass, out var remainder));
        Assert.True(remainder.IsEmpty);
        Assert.Equal(spelling, pitchClass.ToString());
    }

    [Theory]
    [InlineData("C", 0)]
    [InlineData("G", 7)]
    [InlineData("D", 2)]
    [InlineData("A", 9)]
    [InlineData("E", 4)]
    [InlineData("B", 11)]
    [InlineData("F", 5)]
    [InlineData("F#", 6)]
    [InlineData("Gb", 6)]
    [InlineData("C#", 1)]
    [InlineData("Db", 1)]
    [InlineData("E#", 5)]
    [InlineData("Fb", 4)]
    [InlineData("B#", 0)]
    [InlineData("Cb", 11)]
    [InlineData("Cx", 2)]
    [InlineData("Dbb", 0)]
    public void TryParse_YieldsExpectedSemitone(string spelling, int semitone)
    {
        Assert.True(PitchClass.TryParse(spelling, out var pitchClass, out _));
        Assert.Equal(semitone, pitchClass.Semitone);
    }

    [Theory]
    [InlineData("C#4", "C#", "4")]  // prefix parsing: the octave digit remains
    [InlineData("Bb/8", "Bb", "/8")]
    [InlineData("Fx?", "Fx", "?")]
    public void TryParse_LeavesRemainder(string input, string parsed, string rest)
    {
        Assert.True(PitchClass.TryParse(input, out var pitchClass, out var remainder));
        Assert.Equal(parsed, pitchClass.ToString());
        Assert.Equal(rest, remainder.ToString());
    }

    [Theory]
    [InlineData("")]
    [InlineData("H")]   // not a letter A-G
    [InlineData("#C")]  // accidental before letter
    [InlineData("c")]   // lowercase not accepted
    public void TryParse_RejectsInvalidInput(string input)
    {
        Assert.False(PitchClass.TryParse(input, out _, out _));
    }

    [Fact]
    public void NamedConstants_RoundTripThroughParse()
    {
        foreach (var (constant, spelling) in new[]
        {
            (PitchClass.C, "C"), (PitchClass.G, "G"), (PitchClass.D, "D"),
            (PitchClass.FSharp, "F#"), (PitchClass.BFlat, "Bb"),
            (PitchClass.ESharp, "E#"), (PitchClass.CFlat, "Cb"),
            (PitchClass.BSharp, "B#"), (PitchClass.FFlat, "Fb"),
        })
        {
            Assert.Equal(spelling, constant.ToString());
            Assert.True(PitchClass.TryParse(spelling, out var parsed, out _));
            Assert.Equal(constant, parsed);
        }
    }
}
