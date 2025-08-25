using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using WpfMusicPlayer.Models;
using WpfMusicPlayer.Services;

namespace WpfMusicPlayer
{
    public partial class ListeningStatsWindow : Window
    {
        private readonly MusicService _musicService;

        public ListeningStatsWindow(MusicService musicService)
        {
            InitializeComponent();
            _musicService = musicService;
            LoadStatistics();
        }

        private void LoadStatistics()
        {
            try
            {
                LoadWeekendStats();
                LoadMonthlyStats();
                LoadAllTimeStats();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading statistics: {ex.Message}", "Error", 
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void LoadWeekendStats()
        {
            var stats = _musicService.GetWeekendStats();
            
            WeekendSummaryText.Text = $"Weekend Plays: {stats.TotalPlays} songs";
            WeekendTimeText.Text = $"Total Listening Time: {FormatDuration(stats.TotalListeningTime)}";
            
            var rankedSongs = stats.TopSongs.Take(50).Select((song, index) => new SongDisplayItem
            {
                Rank = index + 1,
                SongTitle = string.IsNullOrEmpty(song.SongTitle) ? "Unknown Song" : song.SongTitle,
                Artist = string.IsNullOrEmpty(song.Artist) ? "Unknown Artist" : song.Artist,
                TotalPlays = song.TotalPlays,
                TotalListeningTimeText = FormatDuration(song.TotalListeningTime)
            }).ToList();

            WeekendSongsList.ItemsSource = rankedSongs;
        }

        private void LoadMonthlyStats()
        {
            var stats = _musicService.GetMonthlyStats();
            
            MonthlySummaryText.Text = $"Monthly Plays: {stats.TotalPlays} songs";
            MonthlyTimeText.Text = $"Total Listening Time: {FormatDuration(stats.TotalListeningTime)}";
            
            var rankedSongs = stats.TopSongs.Take(50).Select((song, index) => new SongDisplayItem
            {
                Rank = index + 1,
                SongTitle = string.IsNullOrEmpty(song.SongTitle) ? "Unknown Song" : song.SongTitle,
                Artist = string.IsNullOrEmpty(song.Artist) ? "Unknown Artist" : song.Artist,
                TotalPlays = song.TotalPlays,
                TotalListeningTimeText = FormatDuration(song.TotalListeningTime)
            }).ToList();

            MonthlySongsList.ItemsSource = rankedSongs;
        }

        private void LoadAllTimeStats()
        {
            var stats = _musicService.GetAllTimeStats();
            
            AllTimeSummaryText.Text = $"All Time Plays: {stats.TotalPlays} songs";
            AllTimeTimeText.Text = $"Total Listening Time: {FormatDuration(stats.TotalListeningTime)}";
            
            var rankedSongs = stats.TopSongs.Take(100).Select((song, index) => new SongDisplayItem
            {
                Rank = index + 1,
                SongTitle = string.IsNullOrEmpty(song.SongTitle) ? "Unknown Song" : song.SongTitle,
                Artist = string.IsNullOrEmpty(song.Artist) ? "Unknown Artist" : song.Artist,
                TotalPlays = song.TotalPlays,
                TotalListeningTimeText = FormatDuration(song.TotalListeningTime)
            }).ToList();

            AllTimeSongsList.ItemsSource = rankedSongs;
        }

        private string FormatDuration(TimeSpan duration)
        {
            if (duration.TotalDays >= 1)
            {
                return $"{(int)duration.TotalDays}d {duration.Hours}h {duration.Minutes}m";
            }
            else if (duration.TotalHours >= 1)
            {
                return $"{(int)duration.TotalHours}h {duration.Minutes}m";
            }
            else
            {
                return $"{duration.Minutes}m {duration.Seconds}s";
            }
        }

        private void RefreshButton_Click(object sender, RoutedEventArgs e)
        {
            LoadStatistics();
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }

    public class SongDisplayItem
    {
        public int Rank { get; set; }
        public string SongTitle { get; set; } = string.Empty;
        public string Artist { get; set; } = string.Empty;
        public int TotalPlays { get; set; }
        public string TotalListeningTimeText { get; set; } = string.Empty;
    }
}
