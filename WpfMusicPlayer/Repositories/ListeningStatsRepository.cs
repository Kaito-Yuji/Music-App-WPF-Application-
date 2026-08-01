using System.Collections.Generic;
using System.Linq;
using WpfMusicPlayer.Interfaces;
using WpfMusicPlayer.Models;

namespace WpfMusicPlayer.Repositories
{
    public class ListeningStatsRepository : IListeningStatsRepository
    {
        private readonly JsonFileStore<List<SongListeningRecord>> _store;

        public ListeningStatsRepository(string? dataDirectory = null)
        {
            _store = new JsonFileStore<List<SongListeningRecord>>("listening_stats.json", dataDirectory);
        }

        public List<SongListeningRecord> LoadAll()
        {
            // Records written before stats were keyed by path carry no file path and can never be
            // matched to a song again, so drop them rather than showing blank rows.
            return _store.Load()
                .Where(r => !string.IsNullOrEmpty(r.FilePath))
                .ToList();
        }

        public void SaveAll(List<SongListeningRecord> records) => _store.Save(records);
    }
}
