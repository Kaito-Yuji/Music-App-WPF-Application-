using System;
using WpfMusicPlayer.Models;

namespace WpfMusicPlayer.Services
{
    public interface IListeningStatsService
    {
        /// <summary>Raised with the file path of a song the moment it counts as played.</summary>
        event EventHandler<string>? PlayCounted;

        void OnSongStarted(Song song);
        void OnPositionUpdate(Song? currentSong, TimeSpan currentPosition, TimeSpan totalDuration);
        void OnSongStopped();

        int GetPlayCount(string filePath);

        PeriodStatistics GetWeekendStats();
        PeriodStatistics GetMonthlyStats();
        PeriodStatistics GetAllTimeStats();
    }
}
