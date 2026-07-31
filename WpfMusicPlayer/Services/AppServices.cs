using System;

namespace WpfMusicPlayer.Services
{
    /// <summary>
    /// Composition root. The app is small enough that hand-wiring is clearer than pulling in a
    /// container, but every dependency is still resolved in one place and handed to consumers
    /// through interfaces.
    /// </summary>
    public sealed class AppServices : IDisposable
    {
        public AppServices()
        {
            // One interpreter probe shared by both Python-backed features
            var python = new PythonRunner();

            State = new AppStateService();
            Stats = new ListeningStatsService();
            Mood = new MoodAnalyzerService(python);
            Library = new MusicLibraryService(Stats, Mood);
            Playlists = new PlaylistRepository();
            Playback = new PlaybackService();
            Karaoke = new KaraokeService(new AudioSeparatorService(python), Playback);
        }

        public AppStateService State { get; }
        public IListeningStatsService Stats { get; }
        public IMoodAnalyzerService Mood { get; }
        public IMusicLibraryService Library { get; }
        public IPlaylistRepository Playlists { get; }
        public IPlaybackService Playback { get; }
        public IKaraokeService Karaoke { get; }

        public void Dispose()
        {
            Playback.Dispose();
        }
    }
}
