using System;
using System.Diagnostics;
using WpfMusicPlayer.Interfaces;
using WpfMusicPlayer.Models;

namespace WpfMusicPlayer.Repositories
{
    /// <summary>
    /// Remembers what the listener was doing between runs: library folder, volume, playback modes
    /// and where they were in the queue.
    /// </summary>
    public class AppStateRepository : IAppStateRepository
    {
        private readonly JsonFileStore<AppState> _store;

        public AppStateRepository(string? dataDirectory = null)
        {
            _store = new JsonFileStore<AppState>("app_state.json", dataDirectory);
            State = LoadOrMigrate();
        }

        public AppState State { get; }

        public void Save() => _store.Save(State);

        private AppState LoadOrMigrate()
        {
            var state = _store.Load();

            // First run after the switch to app_state.json: inherit the folder the old
            // user settings file remembered, so the library does not have to be re-scanned.
            if (string.IsNullOrEmpty(state.MusicFolderPath))
                state.MusicFolderPath = ReadLegacyMusicFolder();

            return state;
        }

        private static string ReadLegacyMusicFolder()
        {
            try
            {
                return Properties.Settings.Default.MusicFolderPath ?? string.Empty;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Could not read legacy settings: {ex.Message}");
                return string.Empty;
            }
        }
    }
}
