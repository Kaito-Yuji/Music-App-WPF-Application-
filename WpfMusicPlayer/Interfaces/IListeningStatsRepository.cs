using System.Collections.Generic;
using WpfMusicPlayer.Models;

namespace WpfMusicPlayer.Interfaces
{
    /// <summary>Storage for listening history. Knows nothing about how a play is counted.</summary>
    public interface IListeningStatsRepository
    {
        List<SongListeningRecord> LoadAll();

        void SaveAll(List<SongListeningRecord> records);
    }
}
