// Plays a melody over a chord progression and writes <piece>.wav.
// Both are rendered in the time domain, then mixed by addition.
//
// usage: Tonwerk.Melody [ode-to-joy | scarborough-fair]     (default: ode-to-joy)

const int Rate = 44_100;
const double Volume = 0.25;

// How loud each note is over its lifetime. Sustained = flat (organ-like); Plucked = decaying.
// Change these to hear the difference (or use Envelope.Decaying(seconds) / new Envelope(...)).
var melodyEnvelope = Envelope.Plucked;
var chordEnvelope = Envelope.Decaying(1.2);

var name = args.FirstOrDefault() ?? Pieces.OdeToJoy.Name;
var piece = Pieces.All.FirstOrDefault(p => p.Name == name);
if (piece is null)
{
    Console.Error.WriteLine($"unknown piece '{name}'. available: {string.Join(", ", Pieces.All.Select(p => p.Name))}");
    return 1;
}

var melody = piece.Melody.ParseMelody();
var progression = piece.Harmony.ParseHarmonyBars(piece.Key);

Console.WriteLine($"{piece.Title}, {piece.Bpm} bpm, key {piece.Key}");
Console.WriteLine($"progression: {string.Join(" | ", progression.Select(bar => string.Join(" ", bar.Select(c => $"[{c}]"))))}");

var melodyTrack = Synthesizer.Melody(melody, piece.Bpm, Waveform.Sine, Rate, melodyEnvelope);
var chordTrack = Synthesizer.Progression(progression, piece.Bpm, Waveform.Sine, Rate, piece.BeatsPerBar);
var mix = melodyTrack.Mix(chordTrack);

var path = $"{piece.Name}.wav";
using var writer = new WavWriter(path, Rate);
writer.Write(mix, Volume);
Console.WriteLine($"wrote {path} ({mix.Duration.TotalSeconds:F2} s)");
return 0;
