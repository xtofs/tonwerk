static class Pieces
{
    public static readonly Piece OdeToJoy = new(
        Name: "ode-to-joy",
        Title: "Ode to Joy (Beethoven), C major, 4/4",
        Key: Key.Major(Pitch.C4),
        Bpm: 120,
        BeatsPerBar: 4,
        Melody: """
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
            """,
        // Mostly one chord per bar, two where the melody changes function halfway through.
        //   phrase 1   I | V7 | I | V7         half cadence: bar 4 ends on V7, the phrase stays open
        //   phrase 2   I | V7 | I | V7 I       authentic cadence: the D in bar 8 sits on V7, the final C on I
        //   phrase 3   V7 I | V7 I | V7 | I V7 leans on the dominant, ends open on V7
        //   phrase 4   I | V7 | I | V7 I       same as phrase 2, now closing for good
        Harmony: """
            I     | V7    | I     | V7     |
            I     | V7    | I     | V7 I   |
            V7 I  | V7 I  | V7    | I V7   |
            I     | V7    | I     | V7 I
            """);

    // The traditional English ballad (public domain; not the Simon & Garfunkel arrangement).
    // The pitches follow the widely published tune; the rhythm (one line per four 3/4 bars) and the
    // harmonization are one reasonable choice, easy to change.
    //
    // D Dorian is D minor with a raised sixth (B instead of Bb). The tune lives on the white keys
    // D E F G A B C D, and the major IV chord (G) is what makes it sound Dorian rather than plain minor.
    public static readonly Piece ScarboroughFair = new(
        Name: "scarborough-fair",
        Title: "Scarborough Fair (traditional), D Dorian, 3/4",
        Key: Key.Dorian(Pitch.D5),
        Bpm: 120, // 96,
        BeatsPerBar: 3,
        Melody: """
            D4/2 D/4 | A/2 A/4 | A/4 E F    | E/4 D/2 |
            A/2 C5/4 | D/2 C/4 | A4/4 B G   | A/2. |
            D5/2 D/4 | D/4 C A4/4 | A/4 G F | E/4 C/2 |
            D/2 A/4  | G/4 F E | D/2 C/4    | D/2.
            """,
        Harmony: """
            i   | IV  | i   | i    |
            i   | III | IV  | i    |
            III | IV  | i   | VII  |
            i   | VII | i   | i
            """);

    public static readonly Piece[] All = [OdeToJoy, ScarboroughFair];
}
