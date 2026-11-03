namespace Tonwerk.Tests;

public class PitchTests
{
    [Theory]
    [InlineData("C4", -9)]
    [InlineData("A4", 0)]
    [InlineData("E4", -5)]
    [InlineData("G3", -14)]
    [InlineData("F#3", -15)]
    [InlineData("Bb2", -23)]
    public void TryParse_YieldsExpectedSemitone(string text, int semitone)
    {
        Assert.True(Pitch.TryParse(text, out var pitch));
        Assert.Equal(semitone, pitch.Semitone);
        Assert.Equal(text.Replace("Bb", "A#"), pitch.ToString()); // flats print as sharps
    }

    [Theory]
    [InlineData("E4/4", "E4", "/4")]
    [InlineData("C#3.", "C#3", ".")]
    [InlineData("A4", "A4", "")]
    public void TryParse_Prefix_LeavesRemainder(string input, string parsed, string rest)
    {
        Assert.True(Pitch.TryParse(input, out var pitch, out var remainder));
        Assert.Equal(parsed, pitch.ToString());
        Assert.Equal(rest, remainder.ToString());
    }

    [Fact]
    public void TryParse_OmittedOctave_UsesDefault()
    {
        Assert.True(Pitch.TryParse("E", defaultOctave: 3, out var pitch));
        Assert.Equal("E3", pitch.ToString());
    }

    [Theory]
    [InlineData("")]
    [InlineData("E")]    // octave required without a default
    [InlineData("H4")]   // not a note letter
    [InlineData("E4x")]  // trailing characters (full-span parse)
    public void TryParse_RejectsInvalidInput(string input)
    {
        Assert.False(Pitch.TryParse(input, out _));
    }
}
