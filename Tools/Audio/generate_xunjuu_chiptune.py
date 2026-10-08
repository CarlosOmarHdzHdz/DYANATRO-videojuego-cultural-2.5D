"""Render an original, seamless 8-bit-inspired forest theme for Xunjúu.

No third-party recording or melody is sampled. Requires NumPy and Python 3.
Run from the repository root: python Tools/Audio/generate_xunjuu_chiptune.py
"""

from pathlib import Path
import math
import wave

import numpy as np


SAMPLE_RATE = 44_100
EIGHTH_SECONDS = 0.25  # 120 quarter-note beats per minute, in 6/8.
BAR_COUNT = 24
BAR_SECONDS = 6 * EIGHTH_SECONDS
OUTPUT = Path(__file__).resolve().parents[2] / "Assets/Audio/xunjuu_bosque_chiptune_loop.wav"

# Original A / B / A' phrases. A tilde holds the preceding note; a dot is a rest.
MELODY = [
    "G4 ~ B4 D5 ~ B4", "A4 ~ F#4 A4 D5 ~", "B4 ~ G4 E5 D5 ~", "C5 ~ B4 G4 ~ E4",
    "G4 A4 B4 D5 ~ B4", "A4 ~ F#4 A4 F#4 E4", "E4 G4 C5 ~ B4 A4", "F#4 A4 D5 ~ C5 B4",
    "B4 ~ E5 G5 ~ E5", "D5 ~ C5 B4 G4 ~", "B4 D5 G5 ~ F#5 E5", "F#5 ~ D5 A4 ~ .",
    "E5 ~ G5 B5 ~ G5", "E5 D5 C5 ~ B4 G4", "A4 C5 E5 ~ D5 C5", "A4 ~ F#4 D5 ~ .",
    "G4 ~ B4 D5 ~ G5", "F#5 ~ D5 A4 D5 ~", "E5 ~ B4 G4 B4 ~", "C5 ~ B4 G4 E4 ~",
    "G4 B4 D5 ~ B4 G4", "A4 ~ F#4 A4 D5 ~", "E5 D5 C5 ~ B4 A4", "F#4 A4 D5 ~ F#5 ~",
]

CHORDS = [
    "G", "D", "Em", "C", "G", "D", "C", "D",
    "Em", "C", "G", "D", "Em", "C", "Am", "D",
    "G", "D", "Em", "C", "G", "D", "C", "D",
]

# Root, third, fifth, octave in MIDI note numbers.
VOICINGS = {
    "G": (43, 59, 62, 67), "D": (38, 57, 62, 66),
    "Em": (40, 59, 64, 67), "C": (36, 60, 64, 67),
    "Am": (33, 57, 60, 64),
}


def note_frequency(name: str) -> float:
    semitones = {"C": 0, "C#": 1, "D": 2, "D#": 3, "E": 4, "F": 5,
                 "F#": 6, "G": 7, "G#": 8, "A": 9, "A#": 10, "B": 11}
    pitch = name[:-1]
    octave = int(name[-1])
    midi = 12 * (octave + 1) + semitones[pitch]
    return 440.0 * 2.0 ** ((midi - 69) / 12.0)


def midi_frequency(midi: int) -> float:
    return 440.0 * 2.0 ** ((midi - 69) / 12.0)


def envelope(times: np.ndarray, length: float, attack: float, release: float) -> np.ndarray:
    return np.minimum(1.0, times / attack) * np.minimum(1.0, (length - times) / release).clip(0.0, 1.0)


def add_signal(stereo: np.ndarray, start: float, signal: np.ndarray, volume: float, pan: float) -> None:
    first = round(start * SAMPLE_RATE)
    if first >= len(stereo):
        return
    amount = min(len(signal), len(stereo) - first)
    # Pan slightly for separation; all important material remains audible in mono.
    stereo[first:first + amount, 0] += signal[:amount] * volume * math.sqrt((1 - pan) / 2)
    stereo[first:first + amount, 1] += signal[:amount] * volume * math.sqrt((1 + pan) / 2)


def lead(stereo: np.ndarray, start: float, frequency: float, length: float, volume: float = 0.20) -> None:
    count = max(1, round(length * SAMPLE_RATE))
    t = np.arange(count, dtype=np.float64) / SAMPLE_RATE
    vibrato = 0.003 * np.sin(2 * np.pi * 5.2 * t) * np.minimum(1.0, t / 0.12)
    phase = (frequency * (t + vibrato / (2 * np.pi * 5.2))) % 1.0
    pulse = np.where(phase < 0.31, 1.0, -1.0) + 0.38  # remove pulse DC bias
    triangle = 1.0 - 4.0 * np.abs(phase - 0.5)
    tone = 0.58 * pulse + 0.42 * triangle
    shape = envelope(t, length, 0.012, 0.045) * (0.94 - 0.11 * t / max(length, 0.01))
    add_signal(stereo, start, tone * shape, volume, -0.08)


def pluck(stereo: np.ndarray, start: float, frequency: float, length: float, volume: float = 0.085) -> None:
    count = max(1, round(length * SAMPLE_RATE))
    t = np.arange(count, dtype=np.float64) / SAMPLE_RATE
    phase = (frequency * t) % 1.0
    triangle = 1.0 - 4.0 * np.abs(phase - 0.5)
    tone = 0.72 * triangle + 0.28 * np.sin(2 * np.pi * frequency * t)
    shape = envelope(t, length, 0.004, 0.025) * np.exp(-6.0 * t)
    add_signal(stereo, start, tone * shape, volume, 0.23)


def bass(stereo: np.ndarray, start: float, frequency: float, length: float) -> None:
    count = max(1, round(length * SAMPLE_RATE))
    t = np.arange(count, dtype=np.float64) / SAMPLE_RATE
    phase = (frequency * t) % 1.0
    triangle = 1.0 - 4.0 * np.abs(phase - 0.5)
    tone = 0.76 * triangle + 0.24 * np.sin(2 * np.pi * frequency * t)
    shape = envelope(t, length, 0.009, 0.065) * np.exp(-1.35 * t)
    add_signal(stereo, start, tone * shape, 0.19, 0.0)


def kick(stereo: np.ndarray, start: float, volume: float) -> None:
    duration = 0.16
    t = np.arange(round(duration * SAMPLE_RATE), dtype=np.float64) / SAMPLE_RATE
    phase = 2 * np.pi * (64 * t + 37 * 0.034 * (1 - np.exp(-t / 0.034)))
    tone = np.sin(phase) * np.exp(-30 * t) * np.minimum(1.0, t / 0.002)
    add_signal(stereo, start, tone, volume, 0.0)


def tick(stereo: np.ndarray, start: float, rng: np.random.Generator, volume: float, snare: bool) -> None:
    duration = 0.085 if snare else 0.032
    t = np.arange(round(duration * SAMPLE_RATE), dtype=np.float64) / SAMPLE_RATE
    noise = rng.uniform(-1.0, 1.0, len(t))
    noise = noise - np.convolve(noise, np.ones(13) / 13, mode="same")
    tone = noise * np.exp(-(48 if snare else 105) * t) * np.minimum(1.0, t / 0.002)
    add_signal(stereo, start, tone, volume, 0.12 if snare else -0.12)


def render() -> np.ndarray:
    total_samples = round(BAR_COUNT * BAR_SECONDS * SAMPLE_RATE)
    stereo = np.zeros((total_samples, 2), dtype=np.float64)
    rng = np.random.default_rng(73049)

    for bar_index, (row, harmony) in enumerate(zip(MELODY, CHORDS, strict=True)):
        bar_start = bar_index * BAR_SECONDS
        notes = row.split()
        assert len(notes) == 6, (bar_index, row)
        chord = VOICINGS[harmony]

        # Melody with explicit held notes; all notes release before the next attack.
        for step, symbol in enumerate(notes):
            if symbol in ("~", "."):
                continue
            next_step = step + 1
            while next_step < 6 and notes[next_step] == "~":
                next_step += 1
            length = (next_step - step) * EIGHTH_SECONDS - 0.024
            lead(stereo, bar_start + step * EIGHTH_SECONDS,
                 note_frequency(symbol), length,
                 0.18 if 8 <= bar_index < 16 else 0.20)

        # A light plucked arpeggio suggests a guitar's supporting role without
        # claiming to reproduce any traditional melody or instrument recording.
        for step, voice in enumerate((1, 2, 3, 2, 1, 2)):
            pluck(stereo, bar_start + step * EIGHTH_SECONDS,
                  midi_frequency(chord[voice]), EIGHTH_SECONDS * 0.88,
                  0.080 if step in (0, 3) else 0.064)

        bass(stereo, bar_start, midi_frequency(chord[0]), 0.67)
        bass(stereo, bar_start + 3 * EIGHTH_SECONDS,
             midi_frequency(chord[0] + 7), 0.62)
        kick(stereo, bar_start, 0.11)
        tick(stereo, bar_start + 3 * EIGHTH_SECONDS, rng, 0.035, True)
        for step in (1, 2, 4, 5):
            tick(stereo, bar_start + step * EIGHTH_SECONDS, rng, 0.016, False)

    # A short phrase-end breath prevents a boundary click without adding silence.
    fade_samples = round(0.022 * SAMPLE_RATE)
    stereo[-fade_samples:] *= np.linspace(1.0, 0.0, fade_samples)[:, None]
    stereo -= np.mean(stereo, axis=0, keepdims=True)
    peak = np.max(np.abs(stereo))
    stereo *= 0.78 / peak
    return np.clip(stereo, -1.0, 1.0)


def main() -> None:
    audio = render()
    OUTPUT.parent.mkdir(parents=True, exist_ok=True)
    pcm = (audio * 32767).astype("<i2")
    with wave.open(str(OUTPUT), "wb") as wav:
        wav.setnchannels(2)
        wav.setsampwidth(2)
        wav.setframerate(SAMPLE_RATE)
        wav.writeframes(pcm.tobytes())
    rms = float(np.sqrt(np.mean(audio ** 2)))
    edge_gap = float(np.max(np.abs(audio[0] - audio[-1])))
    print(f"{OUTPUT}\n{len(audio) / SAMPLE_RATE:.3f} s, 44.1 kHz stereo, peak={np.max(np.abs(audio)):.3f}, rms={rms:.3f}, loop edge={edge_gap:.6f}")


if __name__ == "__main__":
    main()
