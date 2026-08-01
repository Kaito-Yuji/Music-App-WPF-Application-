using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using WpfMusicPlayer.Interfaces;
using WpfMusicPlayer.Models;

namespace WpfMusicPlayer.Services
{
    /// <summary>
    /// Single source of truth for "how much has this song been listened to".
    /// A play is counted once the listener passes <see cref="CompletionThreshold"/> of the track.
    /// </summary>
    public class ListeningStatsService : IListeningStatsService
    {
        public const double CompletionThreshold = 0.75;

        /// <summary>
        /// Largest gap that still counts as continuous listening. The position timer ticks every
        /// 100 ms, so anything much larger means the app was paused, minimised or the machine
        /// was asleep - crediting that time would inflate the statistics.
        /// </summary>
        private static readonly TimeSpan MaxCreditPerUpdate = TimeSpan.FromSeconds(1);

        private readonly IListeningStatsRepository _repository;
        private readonly List<SongListeningRecord> _listeningRecords;
        private readonly Dictionary<string, int> _playCounts;

        // Monotonic clock: immune to the wall clock being adjusted mid-session
        private readonly Stopwatch _clock = Stopwatch.StartNew();

        private string _currentFilePath = string.Empty;
        private TimeSpan _lastUpdate;
        private TimeSpan _currentSongSessionTime;
        private bool _currentSongCompleted;

        /// <summary>
        /// Raised the moment a song crosses the completion threshold, carrying its file path.
        /// </summary>
        public event EventHandler<string>? PlayCounted;

        public ListeningStatsService(IListeningStatsRepository repository)
        {
            _repository = repository;

            _listeningRecords = _repository.LoadAll();
            _playCounts = BuildPlayCounts(_listeningRecords);
        }

        #region Public Methods

        /// <summary>
        /// Call this every time playback of a track begins - including replays of the same track,
        /// so that repeat-one sessions are counted separately.
        /// </summary>
        public void OnSongStarted(Song song)
        {
            SaveCurrentSession();

            _currentFilePath = song.FilePath;
            _lastUpdate = _clock.Elapsed;
            _currentSongSessionTime = TimeSpan.Zero;
            _currentSongCompleted = false;
        }

        /// <summary>
        /// Call this from the position timer to track listening progress.
        /// </summary>
        public void OnPositionUpdate(Song? currentSong, TimeSpan currentPosition, TimeSpan totalDuration)
        {
            if (currentSong == null || totalDuration <= TimeSpan.Zero)
                return;

            if (!PathEquals(currentSong.FilePath, _currentFilePath))
                return;

            var now = _clock.Elapsed;
            var elapsed = now - _lastUpdate;
            _lastUpdate = now;

            if (elapsed > TimeSpan.Zero && elapsed <= MaxCreditPerUpdate)
                _currentSongSessionTime += elapsed;

            if (_currentSongCompleted)
                return;

            var progress = currentPosition.TotalSeconds / totalDuration.TotalSeconds;
            if (progress >= CompletionThreshold)
            {
                _currentSongCompleted = true;
                PlayCounted?.Invoke(this, _currentFilePath);
            }
        }

        /// <summary>
        /// Call this when playback stops, the track changes, or the app closes.
        /// </summary>
        public void OnSongStopped()
        {
            SaveCurrentSession();
        }

        /// <summary>
        /// Number of completed plays recorded for a file, across all time.
        /// </summary>
        public int GetPlayCount(string filePath)
        {
            if (string.IsNullOrEmpty(filePath))
                return 0;

            return _playCounts.TryGetValue(Normalise(filePath), out var count) ? count : 0;
        }

        /// <summary>
        /// Weekend statistics (Saturday and Sunday of the current week).
        /// </summary>
        public PeriodStatistics GetWeekendStats()
        {
            var now = DateTime.Now;
            var weekStart = now.Date.AddDays(-(int)now.DayOfWeek);
            var weekendStart = weekStart.AddDays(6);
            var weekendEnd = weekStart.AddDays(7).AddTicks(-1);

            // Before the weekend arrives, report the most recent one
            if (now.DayOfWeek != DayOfWeek.Saturday && now.DayOfWeek != DayOfWeek.Sunday)
            {
                weekendStart = weekendStart.AddDays(-7);
                weekendEnd = weekendEnd.AddDays(-7);
            }

            return GetStatsForPeriod(weekendStart, weekendEnd, "This Weekend");
        }

        /// <summary>
        /// Statistics for the current calendar month.
        /// </summary>
        public PeriodStatistics GetMonthlyStats()
        {
            var now = DateTime.Now;
            var monthStart = new DateTime(now.Year, now.Month, 1);
            var monthEnd = monthStart.AddMonths(1).AddTicks(-1);

            return GetStatsForPeriod(monthStart, monthEnd, $"{now:MMMM yyyy}");
        }

        /// <summary>
        /// All-time statistics.
        /// </summary>
        public PeriodStatistics GetAllTimeStats()
        {
            if (_listeningRecords.Count == 0)
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
            if (string.IsNullOrEmpty(_currentFilePath) || _currentSongSessionTime == TimeSpan.Zero)
                return;

            var record = new SongListeningRecord
            {
                FilePath = _currentFilePath,
                ListenDate = DateTime.Now,
                ListenDuration = _currentSongSessionTime,
                CompletedAt75Percent = _currentSongCompleted
            };

            _listeningRecords.Add(record);

            if (record.CompletedAt75Percent)
            {
                var key = Normalise(record.FilePath);
                _playCounts[key] = _playCounts.TryGetValue(key, out var count) ? count + 1 : 1;
            }

            _repository.SaveAll(_listeningRecords);

            _currentFilePath = string.Empty;
            _currentSongSessionTime = TimeSpan.Zero;
            _currentSongCompleted = false;
        }

        private PeriodStatistics GetStatsForPeriod(DateTime start, DateTime end, string periodName)
        {
            var periodRecords = _listeningRecords
                .Where(r => r.ListenDate >= start && r.ListenDate <= end)
                .ToList();

            var songStats = periodRecords
                .GroupBy(r => Normalise(r.FilePath))
                .Select(g => new SongStatistics
                {
                    FilePath = g.First().FilePath,
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

        private static Dictionary<string, int> BuildPlayCounts(IEnumerable<SongListeningRecord> records)
        {
            return records
                .Where(r => r.CompletedAt75Percent && !string.IsNullOrEmpty(r.FilePath))
                .GroupBy(r => Normalise(r.FilePath))
                .ToDictionary(g => g.Key, g => g.Count());
        }

        private static string Normalise(string filePath) => filePath.ToLowerInvariant();

        private static bool PathEquals(string left, string right) =>
            string.Equals(left, right, StringComparison.OrdinalIgnoreCase);

        #endregion
    }
}
