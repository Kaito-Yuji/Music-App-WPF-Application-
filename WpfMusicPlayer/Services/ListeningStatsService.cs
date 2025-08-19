using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using WpfMusicPlayer.Models;

namespace WpfMusicPlayer.Services
{
    public class ListeningStatsService
    {
        private readonly string _statsFilePath;
        private readonly List<SongListeningRecord> _listeningRecords;
        private DateTime _currentSessionStart;
        private string _currentSongId = string.Empty;
        private TimeSpan _currentSongSessionTime = TimeSpan.Zero;
        private bool _currentSongMarkedAs75Percent = false;

        public ListeningStatsService()
        {
            // Initialize stats file path
            var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            var appFolder = Path.Combine(appDataPath, "WpfMusicPlayer");
            Directory.CreateDirectory(appFolder);
            _statsFilePath = Path.Combine(appFolder, "listening_stats.json");

            _listeningRecords = LoadStatsFromFile();
        }

        #region Public Methods

        /// <summary>
        /// Call this when a song starts playing
        /// </summary>
        public void OnSongStarted(Song song)
        {
            // Save any previous session
            SaveCurrentSession();

            // Start new session
            _currentSongId = song.Id;
            _currentSessionStart = DateTime.Now;
            _currentSongSessionTime = TimeSpan.Zero;
            _currentSongMarkedAs75Percent = false;
        }

        /// <summary>
        /// Call this periodically (from the position timer) to track listening progress
        /// </summary>
        public void OnPositionUpdate(Song currentSong, TimeSpan currentPosition, TimeSpan totalDuration)
        {
            if (currentSong?.Id != _currentSongId || totalDuration == TimeSpan.Zero)
                return;

            var now = DateTime.Now;
            var sessionDuration = now - _currentSessionStart;
            
            // Only count if the session is reasonable (not seeking or paused for too long)
            if (sessionDuration <= TimeSpan.FromMinutes(1)) // Allow up to 1 minute of real-time per update
            {
                _currentSongSessionTime += TimeSpan.FromMilliseconds(100); // Timer interval
            }

            // Check if we've reached 75% of the song
            var progressPercent = currentPosition.TotalSeconds / totalDuration.TotalSeconds;
            if (progressPercent >= 0.75 && !_currentSongMarkedAs75Percent)
            {
                _currentSongMarkedAs75Percent = true;
                // This will be recorded when the session ends
            }

            _currentSessionStart = now;
        }

        /// <summary>
        /// Call this when a song stops, changes, or the app closes
        /// </summary>
        public void OnSongStopped()
        {
            SaveCurrentSession();
        }

        /// <summary>
        /// Get weekend statistics (Saturday and Sunday of current week)
        /// </summary>
        public PeriodStatistics GetWeekendStats()
        {
            var now = DateTime.Now;
            var weekStart = now.Date.AddDays(-(int)now.DayOfWeek); // Start of current week (Sunday)
            var weekendStart = weekStart.AddDays(6); // Saturday
            var weekendEnd = weekStart.AddDays(7).AddTicks(-1); // End of Sunday

            // If it's not weekend yet, get last weekend
            if (now.DayOfWeek != DayOfWeek.Saturday && now.DayOfWeek != DayOfWeek.Sunday)
            {
                weekendStart = weekendStart.AddDays(-7);
                weekendEnd = weekendEnd.AddDays(-7);
            }

            return GetStatsForPeriod(weekendStart, weekendEnd, "This Weekend");
        }

        /// <summary>
        /// Get monthly statistics for the current month
        /// </summary>
        public PeriodStatistics GetMonthlyStats()
        {
            var now = DateTime.Now;
            var monthStart = new DateTime(now.Year, now.Month, 1);
            var monthEnd = monthStart.AddMonths(1).AddTicks(-1);

            return GetStatsForPeriod(monthStart, monthEnd, $"{now:MMMM yyyy}");
        }

        /// <summary>
        /// Get all-time statistics
        /// </summary>
        public PeriodStatistics GetAllTimeStats()
        {
            if (!_listeningRecords.Any())
            {
                return new PeriodStatistics
                {
                    PeriodName = "All Time",
                    PeriodStart = DateTime.Now,
                    PeriodEnd = DateTime.Now
                };
            }

            var firstRecord = _listeningRecords.Min(r => r.ListenDate);
            var lastRecord = _listeningRecords.Max(r => r.ListenDate);

            return GetStatsForPeriod(firstRecord, lastRecord, "All Time");
        }

        #endregion

        #region Private Methods

        private void SaveCurrentSession()
        {
            if (string.IsNullOrEmpty(_currentSongId) || _currentSongSessionTime == TimeSpan.Zero)
                return;

            var record = new SongListeningRecord
            {
                SongId = _currentSongId,
                ListenDate = DateTime.Now,
                ListenDuration = _currentSongSessionTime,
                CompletedAt75Percent = _currentSongMarkedAs75Percent
            };

            _listeningRecords.Add(record);
            SaveStatsToFile();

            // Reset session
            _currentSongId = string.Empty;
            _currentSongSessionTime = TimeSpan.Zero;
            _currentSongMarkedAs75Percent = false;
        }

        private PeriodStatistics GetStatsForPeriod(DateTime start, DateTime end, string periodName)
        {
            var periodRecords = _listeningRecords
                .Where(r => r.ListenDate >= start && r.ListenDate <= end)
                .ToList();

            var songStats = periodRecords
                .GroupBy(r => r.SongId)
                .Select(g => new SongStatistics
                {
                    SongId = g.Key,
                    TotalPlays = g.Count(r => r.CompletedAt75Percent),
                    TotalListeningTime = TimeSpan.FromTicks(g.Sum(r => r.ListenDuration.Ticks)),
                    LastPlayed = g.Max(r => r.ListenDate),
                    FirstPlayed = g.Min(r => r.ListenDate)
                })
                .OrderByDescending(s => s.TotalPlays)
                .ThenByDescending(s => s.TotalListeningTime)
                .ToList();

            return new PeriodStatistics
            {
                TopSongs = songStats,
                TotalListeningTime = TimeSpan.FromTicks(periodRecords.Sum(r => r.ListenDuration.Ticks)),
                TotalPlays = periodRecords.Count(r => r.CompletedAt75Percent),
                PeriodStart = start,
                PeriodEnd = end,
                PeriodName = periodName
            };
        }

        private void SaveStatsToFile()
        {
            try
            {
                var json = JsonSerializer.Serialize(_listeningRecords, new JsonSerializerOptions
                {
                    WriteIndented = true
                });
                File.WriteAllText(_statsFilePath, json);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error saving listening stats: {ex.Message}");
            }
        }

        private List<SongListeningRecord> LoadStatsFromFile()
        {
            try
            {
                if (!File.Exists(_statsFilePath))
                    return new List<SongListeningRecord>();

                var json = File.ReadAllText(_statsFilePath);
                return JsonSerializer.Deserialize<List<SongListeningRecord>>(json) ?? new List<SongListeningRecord>();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading listening stats: {ex.Message}");
                return new List<SongListeningRecord>();
            }
        }

        /// <summary>
        /// Update song metadata in statistics (call when songs are loaded)
        /// </summary>
        public void UpdateSongMetadata(IEnumerable<Song> songs)
        {
            var songDict = songs.ToDictionary(s => s.Id, s => s);

            foreach (var stats in GetAllTimeStats().TopSongs)
            {
                if (songDict.TryGetValue(stats.SongId, out var song))
                {
                    stats.SongTitle = song.Title;
                    stats.Artist = song.Artist;
                    stats.FilePath = song.FilePath;
                }
            }
        }

        #endregion
    }
}
