using System;
using WpfMusicPlayer.Interfaces;
using WpfMusicPlayer.Repositories;

namespace WpfMusicPlayer.Services
{
    /// <summary>
    /// Composition root. The app is small enough that hand-wiring is clearer than pulling in a
    /// container, but every dependency is still resolved in one place and handed to consumers
    /// through interfaces.
    ///
    /// Reading top to bottom also shows the dependency order: repositories know nothing, services
    /// build on repositories, and the view models build on services.
    /// </summary>
    public sealed class AppServices : IDisposable
    {
        public AppServices()
        {
            // Storage
            State = new AppStateRepository();
            Playlists = new PlaylistRepository();

            var statsRepository = new ListeningStatsRepository();
            var moodCache = new MoodCacheRepository();

            // One interpreter probe shared by both Python-backed features
            var python = new PythonRunner();

            // Services
            Stats = new ListeningStatsService(statsRepository);
            Mood = new MoodAnalyzerService(python, moodCache);
            Library = new MusicLibraryService(Stats, Mood);
            Playback = new PlaybackService();
            Karaoke = new KaraokeService(new AudioSeparatorService(python), Playback);
        }

        public IAppStateRepository State { get; }
        public IPlaylistRepository Playlists { get; }

        public IListeningStatsService Stats { get; }
        public IMoodAnalyzerService Mood { get; }
        public IMusicLibraryService Library { get; }
        public IPlaybackService Playback { get; }
        public IKaraokeService Karaoke { get; }

        public void Dispose()
        {
            Playback.Dispose();
        }
    }
}
