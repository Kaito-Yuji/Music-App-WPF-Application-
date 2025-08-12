# Offline Spotify Clone - WPF Music Player

A feature-rich offline music player built with WPF (.NET 8) that provides all the essential features you'd expect from a modern music application.

## Features

### Core Functions ✅

#### File Playback

- **Play, Pause, Stop** - Full playback control
- **Next/Previous Track** - Navigate through your queue
- **Seek (Scrub)** - Click anywhere on the progress bar to jump to that position
- **Repeat Modes** - Normal, Repeat All, Repeat One, and Shuffle
- **Volume Control** - Adjustable volume slider

#### Supported Audio Formats

- MP3, WAV, FLAC, AAC, OGG, M4A, WMA
- Automatic metadata reading (ID3 tags)

#### Local File Browsing

- **Scan Music Folder** - Load music from any folder on your device
- **Metadata Display** - Shows title, artist, album, duration, genre, year
- **Album Art Support** - Displays embedded album artwork
- **Smart Search** - Search by song title, artist, album, or genre

#### Playlist Management

- **Create Playlists** - Build custom playlists
- **Add/Remove Songs** - Drag and drop or use context menus
- **Playlist Library** - View all your playlists in the sidebar
- **Delete Playlists** - Remove unwanted playlists

#### Now Playing Screen

- **Current Song Info** - Title, artist, album display
- **Album Art** - Large album artwork display
- **Playback Controls** - Easy access to all player functions
- **Progress Bar** - Visual representation of playback progress
- **Queue Management** - View and modify the current queue

## How to Use

### Getting Started

1. **Launch the Application** - Run `dotnet run` from the project directory
2. **Scan Your Music** - Click "📁 Scan Music Folder" to load your music library
3. **Browse Your Library** - All discovered songs will appear in the main list

### Playing Music

- **Double-click** any song to start playing
- Use the **play/pause** button (▶️/⏸️) to control playback
- **Previous** (⏮️) and **Next** (⏭️) buttons to navigate
- **Stop** (⏹️) to stop playback completely

### Playlist Management

1. **Create a Playlist** - Click "➕ Create Playlist" in the sidebar
2. **Add Songs** - Click the ➕ button next to any song and select a playlist
3. **View Playlist** - Click on any playlist in the sidebar to view its contents
4. **Delete Playlist** - Click the 🗑️ button next to any playlist

### Search and Navigation

- Use the search box to find songs by title, artist, album, or genre
- Results update in real-time as you type
- Click on playlists in the sidebar to view their contents

### Queue Management

- The right sidebar shows the current queue and now playing info
- **Double-click** songs in the queue to jump to them
- **Remove** songs from queue with the ❌ button

### Playback Modes

Click the mode button repeatedly to cycle through:

- **🔁** - Normal (play through queue once)
- **🔁** - Repeat All (loop the entire queue)
- **🔂** - Repeat One (loop current song)
- **🔀** - Shuffle (random order)

## Technical Features

### Audio Engine

- Built with **NAudio** for high-quality audio playback
- Support for multiple audio formats
- Efficient memory management

### Metadata Handling

- **TagLibSharp** for reading ID3 tags and album art
- Automatic metadata extraction during folder scanning
- Fallback to filename when metadata is missing

### User Interface

- **Dark theme** inspired by Spotify
- **Responsive design** with three-panel layout
- **Real-time updates** for playback progress
- **Modern styling** with hover effects and smooth animations

## System Requirements

- **Operating System**: Windows 10/11
- **.NET Runtime**: .NET 8.0 or higher
- **Audio Codecs**: Windows Media Format SDK (included with Windows)

## Dependencies

- **NAudio** (2.2.1) - Audio playback engine
- **TagLibSharp** (2.3.0) - Metadata reading
- **Microsoft.Toolkit.Mvvm** (7.1.2) - MVVM framework
- **Ookii.Dialogs.Wpf** (5.0.1) - Modern folder browser dialog

## Project Structure

```
WpfMusicPlayer/
├── Models/           # Data models (Song, Playlist, Enums)
├── Services/         # Business logic (AudioService, MusicLibraryService, PlayerService)
├── Converters/       # UI data converters
├── Views/           # Dialog windows
└── MainWindow.*     # Main application window
```

## Building and Running

1. **Clone the repository**
2. **Navigate to the project directory**
   ```bash
   cd WpfMusicPlayer
   ```
3. **Restore dependencies**
   ```bash
   dotnet restore
   ```
4. **Build the project**
   ```bash
   dotnet build
   ```
5. **Run the application**
   ```bash
   dotnet run
   ```

## Future Enhancements

The application is designed to be extensible. Potential future features include:

- Equalizer and audio effects
- Library management and organization
- Crossfade between tracks
- Lyrics display
- Online music streaming integration
- Theme customization
- Keyboard shortcuts
- Mini player mode

## License

This project is open source and available under the MIT License.

---

Enjoy your music! 🎵
