"""Original sample-based acoustic game-loop demo, without chiptune oscillators.

No audio, lyric, or transcribed melody from the user's YouTube link is used.
Needs numpy and tinysoundfont, plus the separately licensed GeneralUser GS bank.
The optional local runtime directory avoids changing the bundled Python runtime.
"""

from pathlib import Path
import argparse
import json
import math
import struct
import sys
import wave

import numpy as np


ROOT = Path(__file__).resolve().parents[2]
SR = 44100
BPM = 96
BAR_FRAMES = int(SR * 60 / BPM * 3)
BAR_SECONDS = BAR_FRAMES / SR
EIGHTH_SECONDS = BAR_SECONDS / 6
TOTAL_BARS = 48

# Original melodic material, not a transcription of La Botella.
# Six eighth-note slots per bar; ~ extends a note; . is a rest.
A = [
    "B4 ~ A4 G4 D5 B4", "A4 G4 E4 G4 ~ .",
    "F#4 A4 C5 D5 ~ A4", "B4 D5 B4 G4 ~ .",
    "G4 ~ B4 E5 D5 B4", "C5 E5 D5 C5 ~ G4",
    "A4 C5 B4 A4 E5 C5", "D5 ~ C5 A4 F#4 .",
    "B4 D5 G5 F#5 D5 B4", "C5 ~ E5 D5 C5 G4",
    "A4 D5 F#5 E5 D5 A4", "G5 ~ D5 B4 G4 .",
    "E5 G5 F#5 E5 B4 G4", "C5 ~ E5 G5 E5 C5",
    "B4 A4 G4 A4 C5 E5", "D5 ~ A4 C5 F#4 A4",
]
B = [
    "B4 E5 G5 F#5 E5 B4", "G5 ~ E5 C5 D5 E5",
    "F#5 E5 D5 A4 ~ C5", "B4 ~ G4 B4 D5 .",
    "E5 ~ B4 G4 A4 B4", "C5 G5 E5 D5 C5 G4",
    "A4 ~ C5 E5 D5 C5", "D5 F#5 E5 D5 A4 .",
    "G5 F#5 E5 B4 E5 G5", "E5 ~ D5 C5 G4 E4",
    "A4 C5 E5 D5 C5 A4", "F#4 ~ A4 D5 E5 F#5",
    "G5 ~ F#5 E5 D5 B4", "C5 E5 G5 E5 D5 C5",
    "B4 ~ G4 A4 C5 E5", "D5 C5 A4 F#4 A4 D5",
]
APRIME = [
    "B4 D5 B4 A4 G4 B4", "C5 ~ G4 E4 G4 C5",
    "D5 A4 F#4 A4 C5 D5", "B4 ~ D5 G5 D5 B4",
    "E5 D5 B4 G4 B4 E5", "G5 E5 D5 C5 ~ E5",
    "A4 C5 E5 C5 B4 A4", "F#4 A4 D5 C5 A4 .",
    "D5 G5 F#5 D5 B4 G4", "E5 ~ C5 G4 C5 E5",
    "D5 ~ A4 C5 D5 F#5", "G5 D5 B4 G4 ~ .",
    "B4 E5 G5 E5 D5 B4", "C5 ~ E5 D5 C5 G4",
    "A4 B4 C5 E5 D5 C5", "A4 C5 D5 ~ F#4 A4",
]
CHORDS_A = ["G", "C", "D", "G", "Em", "C", "Am", "D"] * 2
CHORDS_B = ["Em", "C", "D", "G", "Em", "C", "Am", "D"] * 2
VOICINGS = {
    "G": (43, 55, 59, 62, 67), "C": (36, 55, 60, 64, 67),
    "D": (38, 57, 62, 66, 69), "Em": (40, 55, 59, 64, 67),
    "Am": (45, 57, 60, 64, 69),
}
PROGRAMS = {0: (0, 40), 1: (0, 24), 2: (0, 32), 9: (128, 0)}
LEVELS = {0: .61, 1: .74, 2: .40, 9: .22}
PANS = {0: -.13, 1: .22, 2: -.04, 9: .08}


def midi_pitch(name):
    offsets = {"C": 0, "D": 2, "E": 4, "F": 5, "G": 7, "A": 9, "B": 11}
    return (int(name[-1]) + 1) * 12 + offsets[name[0]] + (1 if "#" in name else 0)


def build_events():
    rng = np.random.default_rng(20261005)
    events = {channel: [] for channel in PROGRAMS}

    def note(channel, start, duration, pitch, velocity, humanize=True):
        jitter = rng.uniform(.002, .012) if humanize else 0
        onset = max(0, round((start + jitter) * SR))
        end = onset + max(1, round(duration * SR))
        vel = int(np.clip(velocity + rng.integers(-4, 5), 1, 110))
        events[channel].extend([(onset, 1, pitch, vel), (end, 0, pitch, 0)])

    melody = A + B + APRIME
    harmony = CHORDS_A + CHORDS_B + CHORDS_A
    for bar, (line, name) in enumerate(zip(melody, harmony, strict=True)):
        base = bar * BAR_SECONDS
        symbols = line.split()
        assert len(symbols) == 6
        for step, symbol in enumerate(symbols):
            if symbol in ("~", "."):
                continue
            stop = step + 1
            while stop < 6 and symbols[stop] == "~":
                stop += 1
            duration = EIGHTH_SECONDS * (stop - step) - .025
            velocity = 77 if step in (0, 3) else 68
            if 16 <= bar < 32:
                velocity -= 5
            note(0, base + step * EIGHTH_SECONDS, duration, midi_pitch(symbol), velocity)

        bass, *chord = VOICINGS[name]
        # Soft nylon-string broken chord, with alternating upper notes.
        pattern = (0, 2, 1, 3, 2, 1) if bar % 2 == 0 else (0, 1, 3, 2, 1, 2)
        for step, voice in enumerate(pattern):
            note(1, base + step * EIGHTH_SECONDS, .53, chord[voice],
                 61 if step in (0, 3) else 49)
        # Two unobtrusive finger-strummed chord accents per bar.
        for step in (0, 3):
            for string, pitch in enumerate(chord[:3]):
                note(1, base + step * EIGHTH_SECONDS + string * .011,
                     .42, pitch, 38 if step == 3 else 41)
        note(2, base, EIGHTH_SECONDS * 2.55, bass, 65)
        note(2, base + 3 * EIGHTH_SECONDS, EIGHTH_SECONDS * 2.45,
             bass + (7 if name != "Am" else -5), 53)

        # Sampled hand percussion, not a drum-machine dance beat.
        note(9, base, .17, 64, 64, False)  # low conga
        note(9, base + 3 * EIGHTH_SECONDS, .15, 63, 50, False)
        for step in (2, 5):
            note(9, base + step * EIGHTH_SECONDS, .09, 76, 33, False)
        if bar % 4 == 3:
            note(9, base + 5.5 * EIGHTH_SECONDS, .08, 77, 26, False)
    return {ch: sorted(items) for ch, items in events.items()}


def render_stem(tinysoundfont, soundfont, channel, events, frames):
    synth = tinysoundfont.Synth(gain=-10, samplerate=SR)
    # Pass bytes because the native Windows loader cannot open accented paths.
    sfid = synth.sfload(soundfont.read_bytes())
    bank, preset = PROGRAMS[channel]
    synth.program_select(channel, sfid, bank, preset, is_drums=channel == 9)
    synth.control_change(channel, 10, 64)
    synth.control_change(channel, 7, 100)
    print(f"Rendering {synth.sfpreset_name(sfid, bank, preset)}", flush=True)
    out = np.zeros((frames + SR * 3, 2), dtype=np.float32)
    cursor = 0
    for pos, kind, pitch, vel in events:
        pos = min(pos, len(out))
        if pos > cursor:
            out[cursor:pos] = np.frombuffer(synth.generate(pos - cursor), dtype=np.float32).reshape(-1, 2)
            cursor = pos
        if kind:
            if not synth.noteon(channel, pitch, vel):
                raise RuntimeError(f"Invalid note {channel} {pitch}")
        else:
            synth.noteoff(channel, pitch)
    if cursor < len(out):
        out[cursor:] = np.frombuffer(synth.generate(len(out) - cursor), dtype=np.float32).reshape(-1, 2)
    # Fold natural instrument releases into the beginning of the cyclic score.
    out[:len(out) - frames] += out[frames:]
    mono = out[:frames].mean(axis=1)
    pan = PANS[channel]
    stereo = np.column_stack((mono * math.sqrt((1 - pan) / 2), mono * math.sqrt((1 + pan) / 2)))
    return stereo * LEVELS[channel]


def write_wav(path, audio):
    with wave.open(str(path), "wb") as wav:
        wav.setnchannels(2)
        wav.setsampwidth(2)
        wav.setframerate(SR)
        wav.writeframes(np.rint(np.clip(audio, -1, 1) * 32767).astype("<i2").tobytes())


def vlq(value):
    result = [value & 127]
    while value >> 7:
        value >>= 7
        result.insert(0, (value & 127) | 128)
    return bytes(result)


def write_midi(path, events):
    # Standard MIDI format 1, usable as editable arrangement in a DAW.
    ppq = 480
    ticks_per_second = ppq * BPM / 60
    tempo = int(60_000_000 / BPM)
    conductor = b"\x00\xff\x51\x03" + tempo.to_bytes(3, "big")
    conductor += b"\x00\xff\x58\x04\x06\x03\x24\x08\x00\xff\x2f\x00"
    tracks = [conductor]
    for ch, items in events.items():
        bank, preset = PROGRAMS[ch]
        data = bytearray(b"\x00" + bytes((0xB0 | ch, 0, 0)))
        data += b"\x00" + bytes((0xC0 | ch, preset))
        last = 0
        for frame, kind, pitch, velocity in items:
            tick = round(frame / SR * ticks_per_second)
            data += vlq(tick - last) + bytes(((0x90 if kind else 0x80) | ch, pitch, velocity))
            last = tick
        data += b"\x00\xff\x2f\x00"
        tracks.append(bytes(data))
    with path.open("wb") as midi:
        midi.write(b"MThd" + struct.pack(">IHHH", 6, 1, len(tracks), ppq))
        for track in tracks:
            midi.write(b"MTrk" + struct.pack(">I", len(track)) + track)


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("--runtime", type=Path, default=ROOT / "tmp/dyanatro_audio_runtime")
    parser.add_argument("--soundfont", type=Path, default=ROOT / "tmp/dyanatro_audio_runtime/GeneralUser-GS.sf2")
    parser.add_argument("--output-dir", type=Path, default=ROOT / "output/Audio/DYANATRO_Hilos_del_camino")
    args = parser.parse_args()
    if not args.soundfont.is_file():
        parser.error("Provide the separately licensed GeneralUser GS SoundFont.")
    sys.path.insert(0, str(args.runtime))
    import tinysoundfont

    frames = TOTAL_BARS * BAR_FRAMES
    events = build_events()
    mix = np.zeros((frames, 2), dtype=np.float32)
    for channel, items in events.items():
        mix += render_stem(tinysoundfont, args.soundfont, channel, items, frames)
    direct = mix.copy()
    # Small cyclic room reflections: no artificial synth pad or long reverb wash.
    for seconds, gain in ((.041, .07), (.083, .054), (.143, .04), (.219, .028)):
        delay = round(seconds * SR)
        mix[:, 0] += gain * np.roll(direct[:, 1], delay)
        mix[:, 1] += gain * np.roll(direct[:, 0], delay + 157)
    mix -= mix.mean(axis=0, keepdims=True)
    mix *= 10 ** (-2.5 / 20) / np.max(np.abs(mix))
    # Eliminate residual DC mismatch at the loop edge over a short release.
    seam = round(.022 * SR)
    mix[-seam:] -= (mix[-1] - mix[0])[None, :] * np.linspace(0, 1, seam)[:, None] ** 2
    assert np.isfinite(mix).all()
    assert np.max(np.abs(mix)) < .99
    args.output_dir.mkdir(parents=True, exist_ok=True)
    loop = args.output_dir / "DYANATRO_hilos_del_camino_loop.wav"
    write_wav(loop, mix)
    preview = mix[:SR * 24].copy()
    preview[:round(.035 * SR)] *= np.linspace(0, 1, round(.035 * SR))[:, None]
    preview[-SR * 2:] *= np.linspace(1, 0, SR * 2)[:, None]
    write_wav(args.output_dir / "DYANATRO_hilos_del_camino_escucha.wav", preview)
    # Audible three-second diagnostic crossing the end/start boundary.
    write_wav(args.output_dir / "DYANATRO_prueba_enlace_bucle.wav",
              np.concatenate((mix[-SR * 3:], mix[:SR * 3])))
    write_midi(args.output_dir / "DYANATRO_hilos_del_camino.mid", events)
    info = {
        "title": "DYANATR’O — Hilos del camino", "type": "original sampled-instrument demo",
        "duration_seconds": len(mix) / SR, "sample_rate": SR,
        "channels": 2, "pcm_bits": 16, "tempo_quarter_note_bpm": BPM,
        "meter": "6/8", "bars": TOTAL_BARS,
        "peak_dbfs": float(20 * np.log10(np.max(np.abs(mix)))),
        "rms_dbfs": float(20 * np.log10(np.sqrt(np.mean(mix ** 2)))),
        "loop_sample_difference": float(np.max(np.abs(mix[-1] - mix[0]))),
        "seam_slope": float(np.max(np.abs(mix[0] - mix[-2]))),
        "clipped_samples": int(np.count_nonzero(np.abs(mix) >= 1)),
        "youtube_reference_audio_downloaded_or_transcribed": False,
        "traditional_mazahua_authenticity_claimed": False,
    }
    (args.output_dir / "comprobacion_audio.json").write_text(json.dumps(info, ensure_ascii=False, indent=2), encoding="utf-8")
    print(json.dumps({"output": str(loop), **info}, ensure_ascii=False, indent=2))


if __name__ == "__main__":
    main()
