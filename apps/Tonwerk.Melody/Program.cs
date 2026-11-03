// Renders a monophonic melody (Ode to Joy) to audio.wav.
// usage: Tonwerk.Melody [--waveform Sine|Square|Sawtooth|Triangle|Gaussian]

const int Rate = 44_100;
const double Bpm = 180;
const double Volume = 0.25;

var waveform = ParseWaveform(args);

// octave and duration are sticky: omitted values repeat the previous note's
var odeToJoy = """
    E4/4 E F G |
    G F E D    |
    C C D E    |
    E/4. D/8 D/2 |

    E/4 E F G |
    G F E D   |
    C C D E   |
    D/4. C/8 C/2 |

    D/4 D E C       |
    D E/8 F E/4 C   |
    D E/8 F E/4 D   |
    C D G3/2        |

    E4/4 E F G |
    G F E D    |
    C C D E    |
    D/4. C/8 C/2
    """.Parse();

var melody = Synthesizer.Melody(odeToJoy, Bpm, waveform, Rate);
var track = Track.Concat(Rate, [melody, Track.Silence(6, Rate)]);

using var writer = new WavWriter("audio.wav", Rate);
writer.Write(track, Volume);
Console.WriteLine($"wrote audio.wav ({waveform}, {track.Duration.TotalSeconds:F2} s)");

static Waveform ParseWaveform(string[] args)
{
    var i = Array.IndexOf(args, "--waveform");
    return i >= 0 && i + 1 < args.Length && Enum.TryParse<Waveform>(args[i + 1], ignoreCase: true, out var w)
        ? w
        : Waveform.Sine;
}
