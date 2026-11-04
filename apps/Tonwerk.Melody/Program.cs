// Plays all 16 bars of Ode to Joy over a chord progression and writes ode-to-joy.wav.
// Both are rendered in the time domain, then mixed by addition.

const int Rate = 44_100;
const double Bpm = 140;
const double Volume = 0.5;

// octave and duration are sticky: omitted values repeat the previous note's
var melody = """
    E4/4 E F G   |
    G F E D      |
    C C D E      |
    E/4. D/8 D/2 |

    E/4 E F G    |
    G F E D      |
    C C D E      |
    D/4. C/8 C/2 |

    D/4 D E C     |
    D E/8 F E/4 C |
    D E/8 F E/4 D |
    C D G3/2      |

    E4/4 E F G   |
    G F E D      |
    C C D E      |
    D/4. C/8 C/2
    """.ParseMelody();

// The harmonic rhythm in C major: mostly one chord per bar, two chords in the
// bars where the melody changes function halfway through.
//
//   phrase 1   I | V7 | I | V7         half cadence: bar 4 ends on V7, the phrase stays open
//   phrase 2   I | V7 | I | V7 I       authentic cadence: the D in bar 8 sits on V7, the final C on I
//   phrase 3   V7 I | V7 I | V7 | I V7 the "bridge": leans on the dominant, ends open on V7
//   phrase 4   I | V7 | I | V7 I       same as phrase 2, now closing for good
//
// Bar lines separate bars; chords within a bar split it equally.
var progression = """
    I     | V7    | I     | V7     |
    I     | V7    | I     | V7 I   |
    V7 I  | V7 I  | V7    | I  V7  |
    I     | V7    | I     | V7 I
    """.ParseHarmonyBars(Key.Major(Pitch.C4));

var melodyTrack = Synthesizer.Melody(melody, Bpm, Waveform.Sine, Rate);
var chordTrack = Synthesizer.Progression(progression, Bpm, Waveform.Sine, Rate);
var mix = melodyTrack.Mix(chordTrack);

using var writer = new WavWriter("ode-to-joy.wav", Rate);
writer.Write(mix, Volume);
Console.WriteLine($"wrote ode-to-joy.wav ({mix.Duration.TotalSeconds:F2} s)");
