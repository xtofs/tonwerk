# Tonwerk

A small .NET playground for sound synthesis: a dependency-free class library
for pitches, notes, chords, tracks, oscillators, noise, and WAV file writing,
plus console apps that render melodies, chord progressions, and colors of noise.
Everything is synthesized in the time domain and mixed by adding samples.
See [docs/THEORY.md](docs/THEORY.md) for the music and signal theory behind it.

## Layout

```plain
tonwerk/
├── src/Tonwerk/             # class library (no package dependencies)
│   ├── Theory/              # Pitch, PitchClass, Interval, Note, Chord, Key, RomanNumeral,
│   │                        #   Harmony, MonophonicMelody (text formats + parsers)
│   ├── Synthesis/           # Waveform, Oscillator, Synthesizer, Noise, Track
│   └── Wav/                 # low-level RIFF/WAV writing
├── apps/
│   ├── Tonwerk.Monophonic/  # monophonic melody, selectable waveform  -> audio.wav
│   ├── Tonwerk.Melody/      # polyphonic: all 16 bars of Ode to Joy over chords -> ode-to-joy.wav
│   └── Tonwerk.Noise/       # colors of noise (time domain, or --spectral via inverse FFT)
├── tests/Tonwerk.Tests/     # xunit tests
├── docs/                    # THEORY.md (pitch classes, intervals, harmony, spectra) and its figures
├── scripts/                 # gen_pitches.py, gen_pitch_classes.cs (single-file C#), plot.py
└── tonwerk.slnx
```

## Melody text format

A monophonic melody is a sequence of notes separated by whitespace; `|` can
be used as a visual bar line (it is ignored by the parser):

```plain
E4/4 E F G |
G F E D    |
C C D E    |
E/4. D/8 D/2
```

Each note is `<pitch>/<duration>`:

* **pitch** — scientific pitch notation: a note letter `A`–`G`, an optional
  accidental (`#` sharp, `b` flat), and an octave digit, e.g. `E4`, `F#3`, `Bb2`.
* **duration** — the denominator of the note value: `/1` whole, `/2` half,
  `/4` quarter, `/8` eighth, `/16` sixteenth, …
  A trailing dot makes it a dotted note (×1.5), e.g. `E4/4.` is a dotted quarter.
* **sticky shorthand** — octave and duration may be omitted; they repeat the
  previous note's values (so `E4/4 E F G` is four quarter notes in octave 4).
  The first note must specify both.

```csharp
Note[] melody = "E4/4 E F G".Parse();
```

## Harmony text format

A chord progression is written as roman numerals relative to a key
(one chord per bar):

```csharp
Chord[] progression = "I V7 I V7".ParseHarmony(Key.Major(Pitch.C4));
// or: Harmony.Parse(Key.Major(Pitch.C4), "I V7 I V7")
```

* The numeral (`I`–`VII`) is the scale degree of the chord's root; its case is
  the chord quality: uppercase = major (`I IV V`), lowercase = minor (`ii iii vi`).
* Suffix `7` adds a seventh: dominant on uppercase (`V7`), minor on lowercase (`ii7`).
* Suffix `°` (or ASCII `o`) makes a diminished triad (`vii°`).
* Minor keys use the natural minor scale; an uppercase `V`/`V7` automatically
  carries the raised leading tone (harmonic-minor dominant).
* Roots are voiced closest to the tonic (in C major, `V` is rooted on the G
  *below* the tonic).

## Core types

* **`Pitch`** — a `record struct` holding semitones relative to A4 = 0
  (twelve-tone equal temperament). Provides `Frequency` (A4 = 440 Hz),
  `Octave`/`PitchClass`, parsing (`Pitch.Parse("F#3")`), and named constants
  for octaves 2–6 (`Pitch.C4`, `Pitch.Cs4`, flats as enharmonic aliases:
  `Pitch.Df4 == Pitch.Cs4`). Arithmetic is affine: `pitch + semitones`
  transposes, but two pitches cannot be added.
* **`Note`** — a `Pitch` plus a duration in integer ticks at 960 ticks per
  quarter note (960 PPQN, as used by MIDI sequencers). The fixed-point
  resolution 960 = 2⁶·3·5 keeps dotted notes, triplets, and quintuplets
  exact. `Beats` and `ToSeconds(bpm)` convert outward.
* **`Track`** — a mono audio track: `double[]` samples in [−1, 1] tied to their
  sample rate, so tracks can only be mixed (`Track.Mix`, weighted superposition
  that cannot clip) or written when the rates match.
* **`WavWriter`** — writes mono 16-bit PCM WAV files on top of a generic
  RIFF chunk writer. All processing code operates on `double[]` tracks in
  [−1, 1]; `Write(track, volume)` is the single place where floating-point
  audio is clamped and converted to integer PCM.
* **`Oscillator` / `Synthesizer`** — `Synthesizer.Melody`, `Chord` and
  `Progression` render notes into tracks with a selectable `Waveform`
  (`Sine`, `Square`, `Sawtooth`, `Triangle`, `Gaussian`) at the exact
  frequencies, with a short fade in/out per tone. Chord voices are summed
  and scaled so they cannot clip.

## Colors of noise

(Why a noise color is a straight line in a spectrum: see [Time domain and frequency domain](docs/THEORY.md#time-domain-and-frequency-domain).)

`Noise.Generate(color, seconds, rate)` makes white, pink, brown, blue and violet
noise sample by sample in the time domain: white noise is random samples; pink
uses Paul Kellet's filter bank (−3 dB/octave); brown is leaky integration (−6);
blue and violet are the first difference of pink and white (+3 / +6 dB/octave).
The output never repeats and can have any length. The tests check the
octave-to-octave slopes.

The noise app can also build the exact *P ∝ 1/f^α* spectrum (`--spectral`):
every audible FFT bin gets a **uniformly random phase** (that is what makes it
noise) and a magnitude *f^(−α/2)*, and an inverse FFT turns it into samples.
That is the only place FftSharp is used, and the result is a single block.
`scripts/plot.py <file.wav>` plots a WAV file's average power spectrum on a
log frequency axis (each noise color is a straight line at its dB/octave
slope). It needs numpy + matplotlib: either `uv run scripts/plot.py noise-pink.wav`
(dependencies are declared inline), or

```sh
python3 -m venv .venv
.venv/bin/pip install -r scripts/requirements.txt
.venv/bin/python scripts/plot.py noise-pink.wav -o out.png
```

## Build and run

```sh
dotnet build tonwerk.slnx
dotnet test tonwerk.slnx
dotnet run --project apps/Tonwerk.Monophonic [-- --waveform Square]   # writes audio.wav
dotnet run --project apps/Tonwerk.Melody                          # writes ode-to-joy.wav (melody over I | V7 | I | V7)
dotnet run --project apps/Tonwerk.Noise [-- --spectral]           # writes noise-white.wav ... noise-violet.wav
./convert.sh audio.wav                                            # optional: convert to audio.m4a (needs ffmpeg)
```

## Regenerating the pitch constants

The named constants in `Theory/Pitch.cs` are generated by a small Python script
(paste its output into the marked section of `Theory/Pitch.cs`):

```sh
python3 scripts/gen_pitches.py        # octaves 2-6 (default)
python3 scripts/gen_pitches.py 0 8    # custom octave range
```
