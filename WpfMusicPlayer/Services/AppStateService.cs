using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text.Json;
using WpfMusicPlayer.Models;

namespace WpfMusicPlayer.Services
{
    /// <summary>
    /// Remembers what the listener was doing between runs: library folder, volume, playback modes
    /// and where they were in the queue.
    /// </summary>
    public class AppStateService
    {
        private readonly string _stateFilePath;

        public AppStateService()
        {
            var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            var appFolder = Path.Combine(appDataPath, "WpfMusicPlayer");
            Directory.CreateDirectory(appFolder);
            _stateFilePath = Path.Combine(appFolder, "app_state.json");

            State = Load();
        }

        public AppState State { get; private set; }

        public void Save()
        {
            try
            {
                var json = JsonSerializer.Serialize(State, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(_stateFilePath, json);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error saving app state: {ex.Message}");
            }
        }

        private AppState Load()
        {
            try
            {
                if (File.Exists(_stateFilePath))
                {
                    var json = File.ReadAllText(_stateFilePath);
                    var state = JsonSerializer.Deserialize<AppState>(json);
                    if (state != null)
                        return state;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error loading app state: {ex.Message}");
            }

            // First run on this machine: inherit the folder the old settings file remembered
            return new AppState
            {
                MusicFolderPath = SafeGetLegacyFolder()
            };
        }

        private static string SafeGetLegacyFolder()
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

    public class AppState
    {
        public string MusicFolderPath { get; set; } = string.Empty;
        public float Volume { get; set; } = 0.8f;
        public bool IsShuffleOn { get; set; }
        public RepeatMode RepeatMode { get; set; } = RepeatMode.Off;

        public List<string> QueueFilePaths { get; set; } = new();
        public int CurrentIndex { get; set; } = -1;
        public double PositionSeconds { get; set; }
    }
}
