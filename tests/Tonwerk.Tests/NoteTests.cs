namespace Tonwerk.Tests;

public class NoteTests
{
    [Theory]
    [InlineData("E4/4", "E4", Note.TicksPerQuarter)]
    [InlineData("D4/2", "D4", 2 * Note.TicksPerQuarter)]
    [InlineData("C4/1", "C4", 4 * Note.TicksPerQuarter)]
    [InlineData("E4/4.", "E4", Note.TicksPerQuarter * 3 / 2)]
    [InlineData("G3/16", "G3", Note.TicksPerQuarter / 4)]
    public void TryParse_YieldsPitchAndTicks(string text, string pitch, int ticks)
    {
        Assert.True(Note.TryParse(text, null, out var note));
        Assert.Equal(pitch, note.Pitch.ToString());
        Assert.Equal(ticks, note.Ticks);
    }

    [Fact]
    public void TryParse_Sticky_TakesOctaveAndDurationFromPrevious()
    {
        Assert.True(Note.TryParse("E4/8", null, out var previous));

        Assert.True(Note.TryParse("F", previous, out var note));
        Assert.Equal("F4", note.Pitch.ToString());
        Assert.Equal(previous.Ticks, note.Ticks);

        Assert.True(Note.TryParse("G3", previous, out note));
        Assert.Equal("G3", note.Pitch.ToString()); // explicit octave wins

        Assert.True(Note.TryParse("A/2", previous, out note));
        Assert.Equal("A4", note.Pitch.ToString());
        Assert.Equal(2 * Note.TicksPerQuarter, note.Ticks); // explicit duration wins
    }

    [Theory]
    [InlineData("E4/4 F G", "E4", " F G")] // prefix parse stops at the separator
    [InlineData("E4/4.x", "E4", "x")]
    public void TryParse_Prefix_LeavesRemainder(string input, string pitch, string rest)
    {
        Assert.True(Note.TryParse(input, out var note, out var remainder));
        Assert.Equal(pitch, note.Pitch.ToString());
        Assert.Equal(rest, remainder.ToString());
    }

    [Theory]
    [InlineData("E/4")]    // first note must have an octave
    [InlineData("E4")]     // first note must have a duration
    [InlineData("E4/0")]   // zero denominator
    [InlineData("E4/7")]   // does not divide the whole note's ticks
    [InlineData("E4/4x")]  // trailing characters (full-span parse)
    public void TryParse_WithoutPrevious_RejectsInvalidInput(string input)
    {
        Assert.False(Note.TryParse(input, null, out _));
    }
}
