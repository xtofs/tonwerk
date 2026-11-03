#!/usr/bin/env python3
"""Generates the named-pitch constants for the Pitch struct (src/Tonwerk/Theory/Pitch.cs).

Emits one block per octave: naturals (C4), sharps (Cs4), and flats as
aliases of their enharmonic sharp (Df4 = Cs4). E and B have no sharp, so
F and C have no flat alias.

Usage: gen_pitches.py [first_octave] [last_octave]   (defaults: 2 6)
"""

import sys

# note offset within the octave (C=0, D=2, E=4, F=5, G=7, A=9, B=11)
NATURALS = [("C", 0), ("D", 2), ("E", 4), ("F", 5), ("G", 7), ("A", 9), ("B", 11)]

first = int(sys.argv[1]) if len(sys.argv) > 1 else 2
last = int(sys.argv[2]) if len(sys.argv) > 2 else 6

blocks = []
for octave in range(first, last + 1):
    lines = []
    for i, (name, offset) in enumerate(NATURALS):
        lines.append(f"    public static readonly Pitch {name}{octave} = FromPitchClassAndOctave({offset}, {octave});")
        if name not in ("E", "B"):  # E and B have no sharp
            lines.append(f"    public static readonly Pitch {name}s{octave} = FromPitchClassAndOctave({offset + 1}, {octave});")
            flat = NATURALS[i + 1][0]  # the next natural's flat is the same pitch
            lines.append(f"    public static readonly Pitch {flat}f{octave} = {name}s{octave};")
    blocks.append("\n".join(lines))

print("\n\n".join(blocks))
