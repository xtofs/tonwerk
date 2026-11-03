#!/usr/bin/env python3
# /// script
# dependencies = ["numpy", "matplotlib"]
# ///
"""Plots the spectra of the generated WAV files.

Left panel:  the noise colors (noise-*.wav) as power spectra on a log frequency
             axis — each color should be a straight line at its dB/octave slope.
Right panel: the chord spectrum (spectrum.wav) with its harmonic spikes.

Files that don't exist are skipped. Run the demo apps first:

    dotnet run --project src/noise
    dotnet run --project src/melody

Dependencies (numpy, matplotlib) — pick one:

    uv run scripts/plot_spectra.py        # uv reads the inline metadata above

or, with a virtual environment (macOS system Python is externally managed,
so installing into it directly won't work):

    python3 -m venv .venv
    .venv/bin/pip install -r scripts/requirements.txt
    .venv/bin/python scripts/plot_spectra.py

Usage:

    plot_spectra.py                        # default demo plot -> spectra.png
    plot_spectra.py -o out.png             # choose the output image
    plot_spectra.py audio.wav              # plot specific WAV file(s) instead
    plot_spectra.py a.wav b.wav -o ab.png
"""

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
        "  uv run scripts/plot_spectra.py\n"
        "  python3 -m venv .venv && .venv/bin/pip install -r scripts/requirements.txt\n"
        "  (then run with .venv/bin/python)"
    )

matplotlib.use("Agg")
import matplotlib.pyplot as plt

N = 65536  # analysis block length

NOISE_COLORS = [
    ("white", "#888888", 0),
    ("pink", "#e377c2", -3),
    ("brown", "#8c564b", -6),
    ("blue", "#1f77b4", 3),
    ("violet", "#9467bd", 6),
]

CHORD_LABELS = [(261.6, "C4"), (329.6, "E4"), (392.0, "G4"), (466.2, "Bb4")]


def read_block(path):
    """Returns (rate, first N samples as float array) of a mono 16-bit WAV."""
    with wave.open(path) as w:
        rate = w.getframerate()
        samples = np.frombuffer(w.readframes(N), dtype=np.int16).astype(float)
    return rate, samples


def band_power(rate, samples):
    """Log-band-averaged power spectrum: returns (band frequencies, band power)."""
    freqs = np.fft.rfftfreq(N, 1 / rate)
    power = np.abs(np.fft.rfft(samples)) ** 2

    # average into log-spaced bands for a readable line
    edges = np.logspace(np.log10(25), np.log10(20_000), 120)
    idx = np.searchsorted(freqs, edges)
    band_f, band_p = [], []
    for i in range(len(edges) - 1):
        lo, hi = idx[i], idx[i + 1]
        if hi > lo:
            band_f.append(np.sqrt(edges[i] * edges[i + 1]))  # geometric center
            band_p.append(power[lo:hi].mean())
    return band_f, band_p


def plot_noise(ax):
    found = False
    for name, color, slope in NOISE_COLORS:
        path = f"noise-{name}.wav"
        if not os.path.exists(path):
            print(f"skipping {path} (not found)")
            continue
        found = True
        rate, samples = read_block(path)
        band_f, band_p = band_power(rate, samples)
        ax.plot(band_f, 10 * np.log10(band_p), color=color, label=f"{name} ({slope:+d} dB/oct)")

    ax.set_xscale("log")
    ax.set_xlabel("frequency [Hz]")
    ax.set_ylabel("power [dB]")
    ax.set_title("noise spectra (one block)")
    if found:
        ax.legend()
    ax.grid(True, which="both", alpha=0.3)


def plot_chord(ax, path="spectrum.wav"):
    if not os.path.exists(path):
        print(f"skipping {path} (not found)")
        ax.set_title(f"{path} not found")
        return
    rate, samples = read_block(path)
    freqs = np.fft.rfftfreq(N, 1 / rate)
    magnitude = np.abs(np.fft.rfft(samples)) / N

    ax.plot(freqs, magnitude, color="#2ca02c", lw=0.8)
    for f, label in CHORD_LABELS:
        bin_ = int(round(f * N / rate))
        ax.annotate(label, (f, magnitude[bin_]), textcoords="offset points",
                    xytext=(0, 8), ha="center", fontsize=9)
    ax.set_xlim(0, 2000)
    ax.set_xlabel("frequency [Hz]")
    ax.set_ylabel("magnitude")
    ax.set_title(f"{path}: chord harmonics")
    ax.grid(True, alpha=0.3)


def plot_wavs(ax, paths):
    """Plots the power spectrum of arbitrary WAV files, one line per file."""
    for path in paths:
        rate, samples = read_block(path)
        band_f, band_p = band_power(rate, samples)
        ax.plot(band_f, 10 * np.log10(np.maximum(band_p, 1e-12)), label=os.path.basename(path))

    ax.set_xscale("log")
    ax.set_xlabel("frequency [Hz]")
    ax.set_ylabel("power [dB]")
    ax.set_title("power spectra (first block)")
    ax.legend()
    ax.grid(True, which="both", alpha=0.3)


def main():
    import argparse

    parser = argparse.ArgumentParser(
        description="Plots WAV power spectra. Without arguments, plots the "
                    "default demo output (noise-*.wav and spectrum.wav).")
    parser.add_argument("wavs", nargs="*",
                        help="WAV files to plot instead of the default demo files")
    parser.add_argument("-o", "--output", default="spectra.png",
                        help="output image (default: %(default)s)")
    args = parser.parse_args()

    if args.wavs:
        fig, ax = plt.subplots(figsize=(8, 5))
        plot_wavs(ax, args.wavs)
    else:
        fig, (ax1, ax2) = plt.subplots(1, 2, figsize=(13, 5))
        plot_noise(ax1)
        plot_chord(ax2)

    plt.tight_layout()
    plt.savefig(args.output, dpi=110)
    print(f"saved {args.output}")


if __name__ == "__main__":
    main()
