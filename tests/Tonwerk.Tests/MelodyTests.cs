namespace Tonwerk.Tests;

public class MelodyTests
{
    [Fact]
    public void Parse_StickyNotation_EqualsFullyWrittenNotation()
    {
        var terse = "E4/4 E F G | G F E D | C C D E | E/4. D/8 D/2".ParseMelody();
        var full = "E4/4 E4/4 F4/4 G4/4 | G4/4 F4/4 E4/4 D4/4 | C4/4 C4/4 D4/4 E4/4 | E4/4. D4/8 D4/2".ParseMelody();
        Assert.Equal(full, terse);
    }

    [Fact]
    public void Parse_OctaveChangeSticks()
    {
        var notes = "C4/4 G3 A B".ParseMelody();
        Assert.Equal(["C4", "G3", "A3", "B3"], notes.Select(n => n.Pitch.ToString()));
    }

    [Fact]
    public void Parse_BarLinesAndNewlinesAreIgnored()
    {
        var withBars = "E4/4|F|G\n\tA".ParseMelody();
        var plain = "E4/4 F G A".ParseMelody();
        Assert.Equal(plain, withBars);
    }

    [Fact]
    public void Parse_EmptyInput_YieldsNoNotes()
    {
        Assert.Empty("  | \n ".ParseMelody());
    }

    [Theory]
    [InlineData("E/4 F G")]   // first note missing the octave
    [InlineData("E4 F G")]    // first note missing the duration
    [InlineData("E4/4 x G")]  // not a note
    [InlineData("E4/4x G")]   // trailing garbage directly after a note
    public void Parse_InvalidInput_Throws(string text)
    {
        var ex = Assert.Throws<FormatException>(() => text.ParseMelody());
        Assert.Contains("Invalid note at", ex.Message);
    }
}
