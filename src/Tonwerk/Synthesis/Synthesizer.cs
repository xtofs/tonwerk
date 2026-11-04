namespace Tonwerk.Synthesis;

/// <summary>
/// Renders notes and chords into <see cref="Track"/>s. Everything happens in the
/// time domain: a voice is an oscillator running at the exact frequency, and
/// simultaneous voices are combined by adding their samples.
/// </summary>
public static class Synthesizer
{
    /// <summary>
    /// One tone at an exact frequency, shaped by an envelope
    /// (<see cref="Envelope.Sustained"/> if none is given).
    /// </summary>
    public static Track Tone(double frequency, double seconds, Waveform waveform, int rate, Envelope? envelope = null)
    {
        var shape = envelope ?? Envelope.Sustained;
        var count = (int)(rate * seconds);
        var samples = new double[count];
        for (var i = 0; i < count; i++)
        {
            var phase = frequency * i / rate % 1.0; // position within the cycle, [0, 1)
            samples[i] = shape.Gain(i, count, rate) * Oscillator.Sample(waveform, phase);
        }
        return new Track(samples, rate);
    }

    /// <summary>A melody: the notes' tones placed one after another.</summary>
    public static Track Melody(IEnumerable<Note> notes, double bpm, Waveform waveform, int rate, Envelope? envelope = null) =>
        Track.Concat(rate, notes.Select(note => Tone(note.Pitch.Frequency, note.ToSeconds(bpm), waveform, rate, envelope)));

    /// <summary>A chord: one tone per pitch, mixed with equal weights so the result cannot clip.</summary>
    public static Track Chord(Chord chord, double seconds, Waveform waveform, int rate, Envelope? envelope = null) =>
        Track.Sum(rate, chord.Pitches.ToArray().Select(p => Tone(p.Frequency, seconds, waveform, rate, envelope)));

    /// <summary>A chord progression: one chord per bar.</summary>
    public static Track Progression(IEnumerable<Chord> chords, double bpm, Waveform waveform, int rate, int beatsPerBar = 4, Envelope? envelope = null) =>
        Track.Concat(rate, chords.Select(chord => Chord(chord, beatsPerBar * 60.0 / bpm, waveform, rate, envelope)));

    /// <summary>
    /// A chord progression with a harmonic rhythm: each bar holds one or more
    /// chords that divide the bar equally (e.g. two chords = half a bar each).
    /// </summary>
    public static Track Progression(Chord[][] bars, double bpm, Waveform waveform, int rate, int beatsPerBar = 4, Envelope? envelope = null) =>
        Track.Concat(rate, bars.SelectMany(bar =>
            bar.Select(chord => Chord(chord, beatsPerBar * 60.0 / bpm / bar.Length, waveform, rate, envelope))));
}
