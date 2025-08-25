using System;
using System.Collections.Generic;

namespace WpfMusicPlayer.Models
{
    public class SongListeningRecord
    {
        public string SongId { get; set; } = string.Empty;
        public DateTime ListenDate { get; set; }
        public TimeSpan ListenDuration { get; set; }
        public bool CompletedAt75Percent { get; set; }
    }

    public class SongStatistics
    {
        public string SongId { get; set; } = string.Empty;
        public string SongTitle { get; set; } = string.Empty;
        public string Artist { get; set; } = string.Empty;
        public string FilePath { get; set; } = string.Empty;
        public int TotalPlays { get; set; }
        public TimeSpan TotalListeningTime { get; set; }
        public DateTime LastPlayed { get; set; }
        public DateTime FirstPlayed { get; set; }
    }

    public class PeriodStatistics
    {
        public List<SongStatistics> TopSongs { get; set; } = new List<SongStatistics>();
        public TimeSpan TotalListeningTime { get; set; }
        public int TotalPlays { get; set; }
        public DateTime PeriodStart { get; set; }
        public DateTime PeriodEnd { get; set; }
        public string PeriodName { get; set; } = string.Empty;
    }

    public enum StatisticsPeriod
    {
        ThisWeekend,
        ThisMonth,
        AllTime
    }
}
