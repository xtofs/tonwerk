/// <summary>A melody with its harmonization, ready to render.</summary>
/// <param name="Name">Command-line name, also used for the output file (<c>name.wav</c>).</param>
/// <param name="Key">The key the roman numerals are relative to; its tonic sets the register of the chords.</param>
/// <param name="Bpm">Tempo in quarter notes per minute.</param>
/// <param name="BeatsPerBar">Quarter notes per bar (4 for 4/4, 3 for 3/4).</param>
/// <param name="Melody">Melody text (see README); octave and duration are sticky.</param>
/// <param name="Harmony">Roman numerals; bar lines separate bars, several chords in a bar share it equally.</param>
record Piece(string Name, string Title, Key Key, double Bpm, int BeatsPerBar, string Melody, string Harmony);
