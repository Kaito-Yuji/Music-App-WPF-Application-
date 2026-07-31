using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.Toolkit.Mvvm.ComponentModel;
using Microsoft.Toolkit.Mvvm.Input;
using WpfMusicPlayer.Models;
using WpfMusicPlayer.Services;

namespace WpfMusicPlayer.ViewModels
{
    public class SongDisplayItem
    {
        public int Rank { get; set; }
        public string SongTitle { get; set; } = string.Empty;
        public string Artist { get; set; } = string.Empty;
        public int TotalPlays { get; set; }
        public string TotalListeningTimeText { get; set; } = string.Empty;
    }

    public class ListeningStatsViewModel : ObservableObject
    {
        private readonly IListeningStatsService _stats;
        private readonly IMusicLibraryService _library;

        private string _weekendSummary = string.Empty;
        private string _weekendTime = string.Empty;
        private string _monthlySummary = string.Empty;
        private string _monthlyTime = string.Empty;
        private string _allTimeSummary = string.Empty;
        private string _allTimeTime = string.Empty;

        public ListeningStatsViewModel(IListeningStatsService stats, IMusicLibraryService library)
        {
            _stats = stats;
            _library = library;

            RefreshCommand = new RelayCommand(Refresh);
            Refresh();
        }

        public RelayCommand RefreshCommand { get; }

        public IReadOnlyList<SongDisplayItem> WeekendSongs { get; private set; } = Array.Empty<SongDisplayItem>();
        public IReadOnlyList<SongDisplayItem> MonthlySongs { get; private set; } = Array.Empty<SongDisplayItem>();
        public IReadOnlyList<SongDisplayItem> AllTimeSongs { get; private set; } = Array.Empty<SongDisplayItem>();

        public string WeekendSummary
        {
            get => _weekendSummary;
            private set => SetProperty(ref _weekendSummary, value);
        }

        public string WeekendTime
        {
            get => _weekendTime;
            private set => SetProperty(ref _weekendTime, value);
        }

        public string MonthlySummary
        {
            get => _monthlySummary;
            private set => SetProperty(ref _monthlySummary, value);
        }

        public string MonthlyTime
        {
            get => _monthlyTime;
            private set => SetProperty(ref _monthlyTime, value);
        }

        public string AllTimeSummary
        {
            get => _allTimeSummary;
            private set => SetProperty(ref _allTimeSummary, value);
        }

        public string AllTimeTime
        {
            get => _allTimeTime;
            private set => SetProperty(ref _allTimeTime, value);
        }

        public void Refresh()
        {
            var weekend = _stats.GetWeekendStats();
            WeekendSummary = $"Weekend Plays: {weekend.TotalPlays} songs";
            WeekendTime = $"Total Listening Time: {FormatDuration(weekend.TotalListeningTime)}";
            WeekendSongs = BuildRanking(weekend, 50);
            OnPropertyChanged(nameof(WeekendSongs));

            var monthly = _stats.GetMonthlyStats();
            MonthlySummary = $"Monthly Plays: {monthly.TotalPlays} songs";
            MonthlyTime = $"Total Listening Time: {FormatDuration(monthly.TotalListeningTime)}";
            MonthlySongs = BuildRanking(monthly, 50);
            OnPropertyChanged(nameof(MonthlySongs));

            var allTime = _stats.GetAllTimeStats();
            AllTimeSummary = $"All Time Plays: {allTime.TotalPlays} songs";
            AllTimeTime = $"Total Listening Time: {FormatDuration(allTime.TotalListeningTime)}";
            AllTimeSongs = BuildRanking(allTime, 100);
            OnPropertyChanged(nameof(AllTimeSongs));
        }

        private IReadOnlyList<SongDisplayItem> BuildRanking(PeriodStatistics stats, int take)
        {
            var byPath = _library.Songs.ToDictionary(s => s.FilePath, StringComparer.OrdinalIgnoreCase);

            return stats.TopSongs.Take(take).Select((entry, index) =>
            {
                byPath.TryGetValue(entry.FilePath, out var song);

                return new SongDisplayItem
                {
                    Rank = index + 1,
                    // Fall back to the file name so a song that has left the library is still
                    // recognisable instead of showing up as "Unknown Song"
                    SongTitle = song?.Title ?? SafeFileName(entry.FilePath),
                    Artist = song?.Artist ?? "Unknown Artist",
                    TotalPlays = entry.TotalPlays,
                    TotalListeningTimeText = FormatDuration(entry.TotalListeningTime)
                };
            }).ToList();
        }

        private static string SafeFileName(string filePath)
        {
            try
            {
                return string.IsNullOrEmpty(filePath) ? "Unknown Song" : Path.GetFileNameWithoutExtension(filePath);
            }
            catch
            {
                return "Unknown Song";
            }
        }

        private static string FormatDuration(TimeSpan duration)
        {
            if (duration.TotalDays >= 1)
                return $"{(int)duration.TotalDays}d {duration.Hours}h {duration.Minutes}m";

            if (duration.TotalHours >= 1)
                return $"{(int)duration.TotalHours}h {duration.Minutes}m";

            return $"{duration.Minutes}m {duration.Seconds}s";
        }
    }
}
