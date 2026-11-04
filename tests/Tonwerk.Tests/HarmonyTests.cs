namespace Tonwerk.Tests;

public class HarmonyTests
{
    /// <summary>All roman-numeral spellings: 7 degrees x 5 qualities.</summary>
    public static TheoryData<string> AllNumerals()
    {
        var data = new TheoryData<string>();
        foreach (var numeral in new[] { "I", "II", "III", "IV", "V", "VI", "VII" })
        {
            data.Add(numeral);                              // major
            data.Add(numeral + "7");                        // dominant seventh
            data.Add(numeral.ToLowerInvariant());           // minor
            data.Add(numeral.ToLowerInvariant() + "7");     // minor seventh
            data.Add(numeral.ToLowerInvariant() + "°");     // diminished
        }
        return data;
    }

    [Theory]
    [MemberData(nameof(AllNumerals))]
    public void RomanNumeral_TryParse_ToString_RoundTrips(string spelling)
    {
        Assert.True(RomanNumeral.TryParse(spelling, out var numeral, out var remainder));
        Assert.True(remainder.IsEmpty);
        Assert.Equal(spelling, numeral.ToString());
    }

    [Fact]
    public void Parse_MajorKey_YieldsExpectedChords()
    {
        var chords = Harmony.Parse(Key.Major(Pitch.C4), "I V7 I V7");
        Assert.Equal("C4 E4 G4", chords[0].ToString());
        Assert.Equal("G3 B3 D4 F4", chords[1].ToString()); // root voiced below the tonic
        Assert.Equal(chords[0].ToString(), chords[2].ToString());
        Assert.Equal(chords[1].ToString(), chords[3].ToString());
    }

    [Fact]
    public void Parse_MinorKey_RaisesLeadingToneInDominant()
    {
        var chords = Harmony.Parse(Key.Minor(Pitch.A3), "i iv V7 i");
        Assert.Equal("A3 C4 E4", chords[0].ToString());
        Assert.Equal("D4 F4 A4", chords[1].ToString());
        Assert.Equal("E3 G#3 B3 D4", chords[2].ToString()); // G#: the raised leading tone
    }

    [Fact]
    public void Parse_Diminished_AsciiAliasEqualsDegreeSign()
    {
        var withSign = Harmony.Parse(Key.Major(Pitch.C4), "vii°")[0];
        var withAlias = Harmony.Parse(Key.Major(Pitch.C4), "viio")[0];
        Assert.Equal("B3 D4 F4", withSign.ToString());
        Assert.Equal(withSign.ToString(), withAlias.ToString());
    }

    [Fact]
    public void Parse_MinorSeventh_YieldsExpectedChord()
    {
        var chord = Harmony.Parse(Key.Major(Pitch.C4), "ii7")[0];
        Assert.Equal("D4 F4 A4 C5", chord.ToString());
    }

    [Fact]
    public void Parse_BarLinesAreIgnored()
    {
        var withBars = Harmony.Parse(Key.Major(Pitch.C4), "I | V7 | I");
        var plain = Harmony.Parse(Key.Major(Pitch.C4), "I V7 I");
        Assert.Equal(plain.Select(c => c.ToString()), withBars.Select(c => c.ToString()));
    }

    [Theory]
    [InlineData("V7x")]   // trailing garbage
    [InlineData("VIII")]  // no such degree
    [InlineData("Vi")]    // mixed case has no defined quality
    [InlineData("x")]     // not a numeral
    [InlineData("I°")]    // diminished numerals are written lowercase
    public void Parse_InvalidInput_Throws(string text)
    {
        var ex = Assert.Throws<FormatException>(() => Harmony.Parse(Key.Major(Pitch.C4), text));
        Assert.Contains("Invalid roman numeral at", ex.Message);
    }
}

public class HarmonyBarsTests
{
    [Fact]
    public void ParseBars_GroupsChordsByBarLine()
    {
        var bars = Harmony.ParseBars(Key.Major(Pitch.C4), "I | V7 I | V7");
        Assert.Equal([1, 2, 1], bars.Select(b => b.Length));
    }

    [Fact]
    public void Progression_WithBars_SplitsEachBarEqually()
    {
        const int rate = 44_100;
        var bars = Harmony.ParseBars(Key.Major(Pitch.C4), "I | V7 I");
        var track = Synthesizer.Progression(bars, bpm: 120, Waveform.Sine, rate);
        Assert.Equal(2 * 4 * 0.5, track.Duration.TotalSeconds, 2); // two 4/4 bars at 120 bpm
    }
}
