namespace Tonwerk.Synthesis;

/// <summary>
/// Renders notes and chords into <see cref="Track"/>s. Everything happens in the
/// time domain: a voice is an oscillator running at the exact frequency, and
/// simultaneous voices are combined by adding their samples.
/// </summary>
public static class Synthesizer
{
    /// <summary>Length of the fade in/out that avoids clicks at note boundaries.</summary>
    const double FadeSeconds = 0.01;

    /// <summary>One tone at an exact frequency, with a short fade in/out.</summary>
    public static Track Tone(double frequency, double seconds, Waveform waveform, int rate)
    {
        var count = (int)(rate * seconds);
        var fade = Math.Max(1, Math.Min((int)(rate * FadeSeconds), count / 2));
        var samples = new double[count];
        for (var i = 0; i < count; i++)
        {
            var envelope = Math.Min(1.0, Math.Min(i, count - 1 - i) / (double)fade);
            var phase = frequency * i / rate % 1.0; // position within the cycle, [0, 1)
            samples[i] = envelope * Oscillator.Sample(waveform, phase);
        }
        return new Track(samples, rate);
    }

    /// <summary>A melody: the notes' tones placed one after another.</summary>
    public static Track Melody(IEnumerable<Note> notes, double bpm, Waveform waveform, int rate) =>
        Track.Concat(rate, notes.Select(note => Tone(note.Pitch.Frequency, note.ToSeconds(bpm), waveform, rate)));

    /// <summary>A chord: one tone per pitch, mixed with equal weights so the result cannot clip.</summary>
    public static Track Chord(Chord chord, double seconds, Waveform waveform, int rate) =>
        Track.Sum(rate, chord.Pitches.ToArray().Select(p => Tone(p.Frequency, seconds, waveform, rate)));

    /// <summary>A chord progression: one chord per bar.</summary>
    public static Track Progression(IEnumerable<Chord> chords, double bpm, Waveform waveform, int rate, int beatsPerBar = 4) =>
        Track.Concat(rate, chords.Select(chord => Chord(chord, beatsPerBar * 60.0 / bpm, waveform, rate)));
}
