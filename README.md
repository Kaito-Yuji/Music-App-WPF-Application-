# 🎵 Music App WPF Application

A desktop music player built with **C# WPF on .NET 8**, with two AI features: splitting a song into
vocals and instrumental so you can sing over the backing track, and estimating the mood of a track
from its audio.

Windows only — WPF targets `net8.0-windows`.

> 🇻🇳 Hướng dẫn sử dụng và kiểm thử từng tính năng bằng tiếng Việt:
> [HUONG-DAN-SU-DUNG.md](HUONG-DAN-SU-DUNG.md)

---

## ✨ Features

### 🎶 Player

- Play, pause, seek, volume.
- Scan a folder for `.mp3`, `.wav`, `.flac`, `.aac`, `.ogg`, `.m4a`, `.wma`, or drag files and
  folders straight onto the window.
- Shuffle and repeat work independently, the way Spotify treats them: shuffle changes the order,
  repeat decides what happens at the ends of it.
- Playlists with cover art, plus tag editing that writes back to the file.
- Volume, queue and playhead position are restored the next time you open the app.

### 🎤 Karaoke — vocal separation (AI)

Runs a Python script that separates a track into `vocals.wav` and `accompaniment.wav` using the
`audio-separator` library and the `UVR-MDX-NET-Inst_HQ_3` ONNX model. Once stems exist, switching
between the original and the instrumental is instant.

You can separate the currently playing song, batch-separate everything in the current view, or
export either stem to a `.wav` file. Separation can be cancelled while it runs.

Stems are cached in `%APPDATA%\WpfMusicPlayer\stems\` so they survive rebuilds.

### 🧠 Mood detection (AI)

Estimates the emotional character of a track from its audio. Analysis runs in Python via
`librosa` and places each song on the two-axis model used in music psychology:

- **valence** — how positive the music sounds, driven mostly by whether the key is major or minor
- **arousal** — how activating it sounds, driven mostly by loudness and tempo

The four quadrants become the four moods:

|  | Positive | Dark |
|---|---|---|
| **High energy** | 🙂 Happy — bright, upbeat | ⚡ Energetic — driving, intense |
| **Low energy** | 🌙 Calm — warm, relaxed | 💧 Sad — slow, subdued |

There is no trained model involved: the verdict comes from measurable features (tempo, key,
loudness, spectral brightness), so it can always be explained rather than just asserted. Hover the
Mood column to see the numbers behind it.

Analyse one song or everything in the current view, then use **🎭 Mood Playlists** to group the
whole library into one playlist per mood. Results are cached, so each song is only analysed once.

> Because it works from acoustic features alone, a slow sad song with a bright arrangement can be
> read as calm. It captures the sound of a track, not its lyrics.

### 📊 Listening statistics

A play is counted once you pass **75%** of a track. Statistics are shown for the weekend, the
current month and all time.

### ⌨️ Shortcuts

| Shortcut | Action |
|---|---|
| `Space` | Play / pause |
| `Ctrl` + `←` / `→` | Previous / next track |
| `Ctrl` + `H` | Toggle shuffle |
| `Ctrl` + `R` | Cycle repeat mode |
| `Ctrl` + `K` | Toggle karaoke mode |
| Media keys | Play/pause, next, previous |

---

## 🚀 Running the app

Only the .NET 8 SDK is needed. The player works fully without Python — only the two AI features
require it.

```bash
dotnet run --project WpfMusicPlayer/WpfMusicPlayer.csproj
```

Or open `WpfMusicPlayer/WpfMusicPlayer.sln` in Visual Studio and press F5.

On first launch, click **📁 Scan Music Folder** and pick your music directory. The path is
remembered.

---

## 🧠 Setting up the AI features

Three things must be installed. The app checks all three and tells you exactly which one is
missing, so if something is wrong the message in the app is the fastest way to find out.

### 1. Python 3.12

Install from [python.org](https://www.python.org/downloads/) and tick **"Add python.exe to PATH"**
during setup.

> **Use 3.12, not the newest release.** On Python 3.14 the `diffq-fixed` dependency has no prebuilt
> wheel, so pip tries to compile it from source and the build fails with
> `'bitpack.pyx' doesn't match any files`.

If `python --version` opens the Microsoft Store instead of reporting a version, turn off the
`python.exe` alias under **Settings → Apps → Advanced app settings → App execution aliases**.

The app runs the literal command `python`, so `py` alone is not enough.

### 2. Python packages

```bash
pip install audio-separator onnxruntime librosa
```

`audio-separator` and `onnxruntime` power vocal separation, `librosa` powers mood detection.

`audio-separator` deliberately leaves `onnxruntime` out of its own dependencies, because the choice
between the CPU and GPU build is yours — which is why it has to be installed explicitly.

### 3. FFmpeg

```bash
winget install --id Gyan.FFmpeg -e
```

Open a **new** terminal afterwards so `PATH` is refreshed, then confirm with `ffmpeg -version`.

### Verifying

```bash
python WpfMusicPlayer/debug_audio_separator.py
```

Four ✓ lines mean separation is ready. This also downloads the 66 MB model on first run, so it
needs an internet connection. Mood detection needs no model download.

Restart the app afterwards — Python is detected once, when the app starts.

> Separation runs on the CPU unless a supported GPU is configured. Budget roughly 5–15 minutes per
> song, and use the progress bar or the Cancel button in the overlay.

---

## 🏗️ Project structure

```
WpfMusicPlayer/
  Models/          Song, Playlist, statistics records, enums
  Services/        One responsibility each, all behind interfaces:
                     PlaybackService     audio engine and queue
                     QueueNavigator      shuffle/repeat rules (pure logic)
                     MusicLibraryService scanning, tags, search
                     PlaylistRepository  playlist storage
                     ListeningStatsService  play counting and history
                     AudioSeparatorService  runs the separation wrapper
                     MoodAnalyzerService    runs the mood detector, caches results
                     PythonRunner        shared interpreter discovery and process plumbing
                     KaraokeService      stem separation + playback swapping
                     AppServices         composition root
  ViewModels/      MainViewModel, PlaylistDetailsViewModel, ListeningStatsViewModel
  Converters/      Value converters used by the XAML
  Resources/       audio_separator_wrapper.py, mood_detector.py
```

The UI follows MVVM: views bind to view models and raise commands, and view models reach the
window system only through `IDialogService`, so no view model depends on WPF window types
directly.

### Where data lives

Everything is under `%APPDATA%\WpfMusicPlayer\`:

| File | Contents |
|---|---|
| `playlists.json` | Playlists (song paths and cached tags) |
| `covers/` | Playlist cover images |
| `listening_stats.json` | Listening history and play counts |
| `app_state.json` | Library folder, volume, queue, playback modes |
| `moods.json` | Cached mood analysis per song |
| `stems/` | Separated vocals and instrumentals |

---

## 🛠️ Built with

- **.NET 8 / WPF** — application and UI
- **NAudio** — playback
- **TagLibSharp** — reading and writing tags
- **Microsoft.Toolkit.Mvvm** — `ObservableObject`, `RelayCommand`
- **Ookii.Dialogs.Wpf** — folder picker
- **audio-separator + onnxruntime + FFmpeg** — vocal separation
- **librosa** — audio feature extraction for mood detection
