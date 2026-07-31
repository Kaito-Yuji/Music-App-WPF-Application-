#!/usr/bin/env python3
"""
Mood detector for WPF Music Player.

Estimates where a track sits on the two-axis model of emotion used throughout music psychology:

    valence  - how positive the music sounds
    arousal  - how activating the music sounds

Those two axes give four quadrants, which are the moods reported back to the app:

                  high arousal
                       |
        Energetic      |      Happy
    (dark, driving)    |   (bright, upbeat)
    -------------------+-------------------  valence
          Sad          |      Calm
    (slow, subdued)    |  (warm, relaxed)
                       |
                  low arousal

Everything is derived from measurable audio features rather than a trained model, so the verdict
can always be explained: which tempo, which key, how loud, how bright.

Prints one line beginning with MOOD_RESULT followed by a JSON object.
"""

import sys
import json
import argparse
import warnings
from pathlib import Path

warnings.filterwarnings("ignore", category=UserWarning)
warnings.filterwarnings("ignore", category=FutureWarning)
warnings.filterwarnings("ignore", category=DeprecationWarning)

RESULT_PREFIX = "MOOD_RESULT "

# Longest excerpt to analyse. A minute and a half from the middle of a track characterises it well
# and keeps analysis to a few seconds per song.
MAX_WINDOW_SECONDS = 90.0

SAMPLE_RATE = 22050

# Ranges each raw measurement is mapped onto 0..1 with. These are calibrated against commercially
# mastered music rather than guessed: modern masters sit far louder than raw recordings do, so a
# textbook RMS range would saturate on every pop track and push the whole library into the
# high-energy half of the mood map.
TEMPO_RANGE = (60.0, 170.0)        # BPM, ballad to drum and bass
LOUDNESS_RANGE = (0.04, 0.32)      # RMS, quiet acoustic to loudness-war master
BRIGHTNESS_RANGE = (900.0, 3200.0)  # spectral centroid in Hz, warm to sibilant

# Weights for combining the measurements.
#
# Tempo carries more of the arousal score than loudness does, because loudness largely reflects
# how a track was mastered rather than how activating the music itself is.
#
# The mode weight has to exceed 0.5 on its own: a slow, warm, unmistakably major piece is the
# definition of calm, and with a smaller weight nothing could lift it out of the negative half no
# matter how clearly major it was.
AROUSAL_WEIGHTS = {"loudness": 0.30, "tempo": 0.50, "brightness": 0.20}
VALENCE_WEIGHTS = {"mode": 0.55, "brightness": 0.25, "tempo": 0.20}

PITCH_CLASSES = ["C", "C#", "D", "D#", "E", "F", "F#", "G", "G#", "A", "A#", "B"]

# Krumhansl-Schmuckler key profiles: how strongly listeners rate each scale degree as belonging
# to a major or a minor key. Correlating a track's pitch content against every rotation of these
# tells us both the key and whether it is major or minor.
MAJOR_PROFILE = [6.35, 2.23, 3.48, 2.33, 4.38, 4.09, 2.52, 5.19, 2.39, 3.66, 2.29, 2.88]
MINOR_PROFILE = [6.33, 2.68, 3.52, 5.38, 2.60, 3.53, 2.54, 4.75, 3.98, 2.69, 3.34, 3.17]


def clamp01(value):
    return max(0.0, min(1.0, float(value)))


def scale_between(value, low, high):
    """Maps value from the range [low, high] onto [0, 1]."""
    if high <= low:
        return 0.0
    return clamp01((value - low) / (high - low))


def estimate_key(np, librosa, y, sr):
    """Returns (key name, 'major'|'minor', confidence 0..1)."""
    chroma = librosa.feature.chroma_cqt(y=y, sr=sr)
    profile = chroma.mean(axis=1)

    if float(profile.sum()) <= 0:
        return "", "", 0.0

    profile = profile / profile.sum()

    major = np.array(MAJOR_PROFILE)
    minor = np.array(MINOR_PROFILE)

    best = (-2.0, 0, "major")

    for tonic in range(12):
        rotated = np.roll(profile, -tonic)

        for name, reference in (("major", major), ("minor", minor)):
            correlation = float(np.corrcoef(rotated, reference)[0, 1])
            if correlation > best[0]:
                best = (correlation, tonic, name)

    correlation, tonic, scale = best

    return PITCH_CLASSES[tonic], scale, clamp01((correlation + 1.0) / 2.0)


def analyse(path):
    import numpy as np
    import librosa

    print("Progress: 10%", flush=True)
    print("Loading audio...", flush=True)

    total_duration = librosa.get_duration(path=str(path))
    window = min(MAX_WINDOW_SECONDS, total_duration)

    # Take the excerpt from the middle: intros and fade-outs are rarely representative
    offset = max(0.0, (total_duration - window) / 2.0)

    y, sr = librosa.load(str(path), sr=SAMPLE_RATE, mono=True, offset=offset, duration=window)

    if y.size == 0:
        raise ValueError("The audio file produced no samples")

    print("Progress: 35%", flush=True)
    print("Measuring tempo and energy...", flush=True)

    tempo, _ = librosa.beat.beat_track(y=y, sr=sr)
    tempo = float(np.atleast_1d(tempo)[0])

    loudness = float(librosa.feature.rms(y=y).mean())
    brightness = float(librosa.feature.spectral_centroid(y=y, sr=sr).mean())

    print("Progress: 65%", flush=True)
    print("Estimating key...", flush=True)

    key, scale, key_confidence = estimate_key(np, librosa, y, sr)

    print("Progress: 85%", flush=True)
    print("Scoring mood...", flush=True)

    # Normalise each measurement onto 0..1
    tempo_score = scale_between(tempo, *TEMPO_RANGE)
    energy_score = scale_between(loudness, *LOUDNESS_RANGE)
    brightness_score = scale_between(brightness, *BRIGHTNESS_RANGE)

    # A major key pulls positivity up, a minor key pulls it down, and the pull is proportional to
    # how confident the key estimate is - an ambiguous key should not dominate the verdict.
    if scale == "major":
        mode_score = 0.5 + 0.5 * key_confidence
    elif scale == "minor":
        mode_score = 0.5 - 0.5 * key_confidence
    else:
        mode_score = 0.5

    # Arousal is mostly about how fast and loud the music is
    arousal = (AROUSAL_WEIGHTS["loudness"] * energy_score
               + AROUSAL_WEIGHTS["tempo"] * tempo_score
               + AROUSAL_WEIGHTS["brightness"] * brightness_score)

    # Valence leans on the key, then on timbre and pace
    valence = (VALENCE_WEIGHTS["mode"] * mode_score
               + VALENCE_WEIGHTS["brightness"] * brightness_score
               + VALENCE_WEIGHTS["tempo"] * tempo_score)

    if arousal >= 0.5:
        mood = "Happy" if valence >= 0.5 else "Energetic"
    else:
        mood = "Calm" if valence >= 0.5 else "Sad"

    print("Progress: 100%", flush=True)

    return {
        "mood": mood,
        "valence": round(clamp01(valence), 4),
        "arousal": round(clamp01(arousal), 4),
        "tempo": round(tempo, 2),
        "key": key,
        "scale": scale,
        "loudness": round(loudness, 5),
        "brightness": round(brightness, 1),
        "analysedSeconds": round(window, 1),
    }


def main():
    parser = argparse.ArgumentParser(description="Estimate the mood of an audio file")
    parser.add_argument("input_file", help="Audio file to analyse")

    args = parser.parse_args()
    input_file = Path(args.input_file)

    if not input_file.exists():
        print(f"Error: Input file '{input_file}' does not exist", file=sys.stderr)
        sys.exit(1)

    try:
        import librosa  # noqa: F401
    except ImportError:
        print("Error: librosa is not installed", file=sys.stderr)
        print("Install it with: pip install librosa", file=sys.stderr)
        sys.exit(1)

    try:
        result = analyse(input_file)
    except Exception as error:  # noqa: BLE001 - the caller only needs the message
        print(f"Error analysing '{input_file.name}': {error}", file=sys.stderr)
        import traceback
        traceback.print_exc(file=sys.stderr)
        sys.exit(1)

    # A single, clearly marked line: librosa and its dependencies are chatty, and the caller has
    # to be able to pick the payload out of whatever else lands on stdout.
    print(RESULT_PREFIX + json.dumps(result), flush=True)


if __name__ == "__main__":
    main()
