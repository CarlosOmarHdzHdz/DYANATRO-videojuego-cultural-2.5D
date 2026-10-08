"""Compose Xunjúu's original acoustic-inspired exploration loop.

No external recording, soundfont, or protected melody is used. The generated
instruments are additive string/flute models and softly synthesized percussion.
Run with: python Tools/Audio/generate_xunjuu_sendero.py
Requires NumPy. Designed to repeat in Unity's existing looping AudioSource.
"""

from pathlib import Path
import math
import wave

import numpy as np


SAMPLE_RATE = 44_100
EIGHTH_SECONDS = 60 / 94 / 2
BAR_SECONDS = 6 * EIGHTH_SECONDS
OUTPUT = Path(__file__).resolve().parents[2] / "Assets/Audio/xunjuu_sendero_acustico_loop.wav"

# 24 original 6/8 bars: A (strings), B (flute), A' (strings and flute).
# A tilde sustains the preceding pitch; a period is a rest.
MELODY = [
    "D5 ~ B4 G4 ~ .", "E5 ~ D5 C5 ~ G4", "F#4 A4 D5 ~ C5 A4", "B4 ~ G4 D5 ~ .",
    "G4 B4 E5 ~ D5 B4", "C5 ~ E5 G5 ~ E5", "A4 ~ C5 E5 D5 C5", "A4 ~ F#4 D5 ~ .",
    "G5 ~ F#5 D5 ~ B4", "E5 ~ G5 B5 ~ G5", "G5 E5 D5 C5 ~ E5", "F#5 ~ E5 D5 A4 ~",
    "E5 ~ G5 E5 ~ C5", "D5 B4 G4 B4 D5 ~", "C5 ~ B4 A4 ~ C5", "F#4 A4 D5 ~ . .",
    "D5 ~ B4 G4 B4 D5", "E5 ~ D5 C5 ~ G4", "G4 B4 E5 ~ G5 E5", "E5 D5 C5 ~ G4 .",
    "B4 ~ D5 G5 ~ D5", "F#5 ~ D5 A4 D5 ~", "E5 D5 C5 ~ B4 A4", "F#4 A4 D5 ~ F#5 ~",
]

HARMONY = [
    "G", "C", "D", "G", "Em", "C", "Am", "D",
    "G", "Em", "C", "D", "C", "G", "Am", "D",
    "G", "C", "Em", "C", "G", "D", "C", "D",
]

# Bass, inner chord, fifth, and upper voice in MIDI note numbers.
VOICINGS = {
    "G": (43, 59, 62, 67),
    "C": (36, 60, 64, 67),
    "D": (38, 57, 62, 66),
    "Em": (40, 59, 64, 67),
    "Am": (33, 57, 60, 64),
}


def pitch_frequency(name: str) -> float:
    semitone = {"C": 0, "C#": 1, "D": 2, "D#": 3, "E": 4, "F": 5,
                "F#": 6, "G": 7, "G#": 8, "A": 9, "A#": 10, "B": 11}
    midi = 12 * (int(name[-1]) + 1) + semitone[name[:-1]]
    return midi_frequency(midi)


def midi_frequency(midi: int) -> float:
    return 440.0 * 2 ** ((midi - 69) / 12)


def shape(t: np.ndarray, duration: float, attack: float, release: float) -> np.ndarray:
    return np.minimum(1.0, t / attack) * np.minimum(1.0, np.maximum(0.0, duration - t) / release)


def place(bus: np.ndarray, start: float, sound: np.ndarray, volume: float, pan: float = 0.0) -> None:
    pos = round(start * SAMPLE_RATE) % len(bus)
    scaled = np.stack((sound * volume * math.sqrt((1 - pan) / 2),
                       sound * volume * math.sqrt((1 + pan) / 2)), axis=1)
    end = min(len(bus), pos + len(sound))
    bus[pos:end] += scaled[:end - pos]
    if end - pos < len(sound):
        bus[:len(sound) - (end - pos)] += scaled[end - pos:]


def bowed_string(bus: np.ndarray, start: float, frequency: float, duration: float, volume: float) -> None:
    t = np.arange(round(duration * SAMPLE_RATE), dtype=np.float64) / SAMPLE_RATE
    vibrato = 0.0034 * (1 - np.exp(-t * 14)) * np.sin(2 * np.pi * 5.1 * t)
    cycles = frequency * (t + vibrato / (2 * np.pi * 5.1))
    sound = np.zeros_like(t)
    for harmonic in range(1, 10):
        sound += (0.68 ** (harmonic - 1) / harmonic ** 0.55) * np.sin(
            2 * np.pi * harmonic * cycles + 0.16 * harmonic
        )
    sound *= shape(t, duration, 0.075, 0.125) * (0.87 + 0.08 * np.sin(2 * np.pi * 1.7 * t))
    place(bus, start, sound, volume, -0.16)


def flute(bus: np.ndarray, start: float, frequency: float, duration: float, volume: float) -> None:
    t = np.arange(round(duration * SAMPLE_RATE), dtype=np.float64) / SAMPLE_RATE
    vibrato = 0.0028 * (1 - np.exp(-t * 9)) * np.sin(2 * np.pi * 4.8 * t)
    cycles = frequency * (t + vibrato / (2 * np.pi * 4.8))
    sound = (np.sin(2 * np.pi * cycles) + 0.18 * np.sin(4 * np.pi * cycles)
             + 0.055 * np.sin(6 * np.pi * cycles))
    sound *= shape(t, duration, 0.060, 0.11)
    place(bus, start, sound, volume, 0.12)


def nylon_string(bus: np.ndarray, start: float, frequency: float, duration: float, volume: float,
                 pan: float) -> None:
    t = np.arange(round(duration * SAMPLE_RATE), dtype=np.float64) / SAMPLE_RATE
    sound = np.zeros_like(t)
    for harmonic in range(1, 11):
        coefficient = abs(math.sin(math.pi * harmonic * 0.19)) / harmonic ** 1.28
        decay = np.exp(-t * (2.25 + 0.48 * harmonic))
        sound += coefficient * decay * np.sin(2 * np.pi * harmonic * frequency * t)
    sound *= shape(t, duration, 0.005, 0.10)
    place(bus, start, sound, volume, pan)


def hand_drum(bus: np.ndarray, start: float, volume: float, rng: np.random.Generator,
              high: bool = False) -> None:
    duration = 0.18 if high else 0.27
    t = np.arange(round(duration * SAMPLE_RATE), dtype=np.float64) / SAMPLE_RATE
    fundamental = 112 if high else 76
    phase = 2 * np.pi * (fundamental * t + 18 * 0.036 * (1 - np.exp(-t / 0.036)))
    skin = rng.uniform(-1, 1, len(t))
    skin = np.convolve(skin, np.ones(15) / 15, mode="same")
    tone = (np.sin(phase) * np.exp(-(25 if high else 15) * t)
            + 0.28 * skin * np.exp(-55 * t))
    tone *= np.minimum(1.0, t / 0.002)
    place(bus, start, tone, volume, -0.03 if high else 0.03)


def shaker(bus: np.ndarray, start: float, rng: np.random.Generator, volume: float) -> None:
    t = np.arange(round(0.085 * SAMPLE_RATE), dtype=np.float64) / SAMPLE_RATE
    noise = rng.uniform(-1, 1, len(t))
    smooth = np.convolve(noise, np.ones(9) / 9, mode="same")
    sound = (noise - smooth) * np.exp(-48 * t) * np.minimum(1.0, t / 0.003)
    place(bus, start, sound, volume, 0.15)


def render() -> np.ndarray:
    assert len(MELODY) == len(HARMONY) == 24
    sample_count = round(len(MELODY) * BAR_SECONDS * SAMPLE_RATE)
    direct = np.zeros((sample_count, 2), dtype=np.float64)
    rng = np.random.default_rng(72181)

    for bar, (row, chord_name) in enumerate(zip(MELODY, HARMONY, strict=True)):
        beginning = bar * BAR_SECONDS
        symbols = row.split()
        assert len(symbols) == 6, (bar, row)
        chord = VOICINGS[chord_name]

        for step, symbol in enumerate(symbols):
            if symbol in ("~", "."):
                continue
            next_step = step + 1
            while next_step < 6 and symbols[next_step] == "~":
                next_step += 1
            duration = (next_step - step) * EIGHTH_SECONDS - 0.012
            start = beginning + step * EIGHTH_SECONDS
            frequency = pitch_frequency(symbol)
            if 8 <= bar < 16:
                flute(direct, start, frequency, duration, 0.155)
            else:
                bowed_string(direct, start, frequency, duration, 0.096)
                if bar >= 16 and step == 0:
                    flute(direct, start, frequency / 2, duration, 0.040)

        # Two slightly distinct plucked patterns keep the scenery lively but
        # leave enough space for dialog, animal calls, and footstep effects.
        arpeggio = (1, 2, 3, 2, 1, 2) if bar % 2 == 0 else (1, 3, 2, 1, 2, 3)
        for step, voice in enumerate(arpeggio):
            nylon_string(direct, beginning + step * EIGHTH_SECONDS,
                         midi_frequency(chord[voice]), EIGHTH_SECONDS * 0.92,
                         0.070 if step in (0, 3) else 0.057, 0.25)

        nylon_string(direct, beginning, midi_frequency(chord[0]),
                     3 * EIGHTH_SECONDS * 0.88, 0.17, -0.08)
        nylon_string(direct, beginning + 3 * EIGHTH_SECONDS,
                     midi_frequency(chord[0] + 7),
                     3 * EIGHTH_SECONDS * 0.85, 0.135, -0.08)

        hand_drum(direct, beginning, 0.10, rng)
        hand_drum(direct, beginning + 3 * EIGHTH_SECONDS, 0.068, rng, True)
        for step in (1, 2, 4, 5):
            shaker(direct, beginning + step * EIGHTH_SECONDS, rng, 0.013)

    # Circular, low-level room reflections also carry the preceding bar's tail
    # into the start of the file, so Unity can loop the file without a gap.
    wet = np.zeros_like(direct)
    reflections = ((0.101, 0.084), (0.179, 0.058), (0.269, 0.048),
                   (0.411, 0.034), (0.587, 0.026))
    for delay_seconds, gain in reflections:
        frames = round(delay_seconds * SAMPLE_RATE)
        wet[:, 0] += gain * np.roll(direct[:, 1], frames)
        wet[:, 1] += gain * np.roll(direct[:, 0], frames + 367)
    mix = direct + wet
    mix -= np.mean(mix, axis=0, keepdims=True)
    mix *= 0.82 / np.max(np.abs(mix))
    # Correct the tiny residual sample offset left by the room reflections.
    # The adjustment is spread across a short musical release, not a hard cut.
    seam = round(0.12 * SAMPLE_RATE)
    correction = (mix[-1] - mix[0])[None, :] * np.linspace(0, 1, seam)[:, None] ** 2
    mix[-seam:] -= correction
    return np.clip(mix, -1.0, 1.0)


def main() -> None:
    audio = render()
    OUTPUT.parent.mkdir(parents=True, exist_ok=True)
    with wave.open(str(OUTPUT), "wb") as wav:
        wav.setnchannels(2)
        wav.setsampwidth(2)
        wav.setframerate(SAMPLE_RATE)
        wav.writeframes((audio * 32767).astype("<i2").tobytes())
    print(f"{OUTPUT}\n{len(audio) / SAMPLE_RATE:.3f} s; peak={np.max(np.abs(audio)):.3f}; "
          f"rms={np.sqrt(np.mean(audio ** 2)):.3f}; edge={np.max(np.abs(audio[0] - audio[-1])):.6f}")


if __name__ == "__main__":
    main()
