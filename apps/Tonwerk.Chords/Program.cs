// Plays a melody over a chord progression and writes ode-to-joy.wav.
// Both are rendered in the time domain, then mixed by addition.

const int Rate = 44_100;
const double Bpm = 120;
const double Volume = 0.25;

// the first phrase of Ode to Joy
// (octave and duration are sticky: omitted values repeat the previous note's)
var melody = """
    E4/4 E  F G |
    G F  E D    |
    C C  D E    |
    E/4. D/8 D/2
    """.Parse();

// one chord per bar (the "harmonic rhythm"), as roman numerals in C major.
// Textbook harmonization of this phrase: I | V7 | I | V7
// (ending on V7 = half cadence, which is why the phrase feels open).
var progression = "I V7 I V7".ParseHarmony(Key.Major(Pitch.C4));
Console.WriteLine($"progression: {string.Join(" | ", progression)}");

var melodyTrack = Synthesizer.Melody(melody, Bpm, Waveform.Sine, Rate);
var chordTrack = Synthesizer.Progression(progression, Bpm, Waveform.Triangle, Rate);
var mix = melodyTrack.Mix(chordTrack);

using var writer = new WavWriter("ode-to-joy.wav", Rate);
writer.Write(mix, Volume);
Console.WriteLine($"wrote ode-to-joy.wav ({mix.Duration.TotalSeconds:F2} s)");
