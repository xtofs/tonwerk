#!/usr/bin/env python3
# /// script
# dependencies = ["numpy", "matplotlib"]
# ///
"""Plots the power spectrum of a WAV file (mono, 16-bit PCM).

The whole file is cut into blocks, each block is windowed and transformed, and
the block spectra are averaged — so noise shows up as a smooth line (a noise
color is a straight line at its dB/octave slope) and music shows its pitches
as peaks. The frequency axis is logarithmic.

Usage:

    uv run scripts/plot.py audio.wav               # -> audio.png
    uv run scripts/plot.py noise-pink.wav -o out.png

uv reads the dependencies (numpy, matplotlib) from the inline metadata above.
Without uv, use a virtual environment (macOS system Python is externally
managed, so installing into it directly won't work):

    python3 -m venv .venv
    .venv/bin/pip install -r scripts/requirements.txt
    .venv/bin/python scripts/plot.py audio.wav
"""

import argparse
import os
import sys
import wave

try:
    import numpy as np
    import matplotlib
except ImportError as e:
    sys.exit(
        f"missing dependency: {e.name}\n"
        "install with one of:\n"
        "  uv run scripts/plot.py <file.wav>\n"
        "  python3 -m venv .venv && .venv/bin/pip install -r scripts/requirements.txt\n"
        "  (then run with .venv/bin/python)"
    )

matplotlib.use("Agg")
import matplotlib.pyplot as plt

N = 16384  # block length: ~0.37 s at 44.1 kHz, 2.7 Hz resolution


def read_wav(path):
    """Returns (rate, samples as floats in [-1, 1]) of a mono 16-bit WAV."""
    with wave.open(path) as w:
        if w.getnchannels() != 1 or w.getsampwidth() != 2:
            sys.exit(f"{path}: only mono 16-bit PCM is supported")
        rate = w.getframerate()
        samples = np.frombuffer(w.readframes(w.getnframes()), dtype=np.int16)
    return rate, samples.astype(float) / 32768


def average_spectrum(rate, samples):
    """Returns (frequencies, power in dB), averaged over all blocks of the file."""
    window = np.hanning(N)
    blocks = max(1, len(samples) // N)
    power = np.zeros(N // 2 + 1)
    for i in range(blocks):
        block = samples[i * N:(i + 1) * N]
        power += np.abs(np.fft.rfft(block * window[:len(block)], N)) ** 2
    power /= blocks
    return np.fft.rfftfreq(N, 1 / rate), 10 * np.log10(np.maximum(power, 1e-20))


def main():
    parser = argparse.ArgumentParser(description="Plots the power spectrum of a WAV file.")
    parser.add_argument("wav", help="mono 16-bit PCM WAV file")
    parser.add_argument("-o", "--output", help="output image (default: <wav name>.png)")
    args = parser.parse_args()

    output = args.output or os.path.splitext(args.wav)[0] + ".png"
    rate, samples = read_wav(args.wav)
    freqs, db = average_spectrum(rate, samples)

    fig, ax = plt.subplots(figsize=(9, 5))
    ax.plot(freqs[1:], db[1:], lw=0.8)
    ax.set_xscale("log")
    ax.set_xlim(25, rate / 2)
    ax.set_xlabel("frequency [Hz]")
    ax.set_ylabel("power [dB]")
    ax.set_title(f"{os.path.basename(args.wav)}: average power spectrum")
    ax.grid(True, which="both", alpha=0.3)

    plt.tight_layout()
    plt.savefig(output, dpi=110)
    print(f"saved {output}")


if __name__ == "__main__":
    main()
