using System.Collections.Generic;
using WpfMusicPlayer.Interfaces;
using WpfMusicPlayer.Models;

namespace WpfMusicPlayer.Repositories
{
    public class MoodCacheRepository : IMoodCacheRepository
    {
        private readonly JsonFileStore<Dictionary<string, MoodAnalysis>> _store;

        public MoodCacheRepository(string? dataDirectory = null)
        {
            _store = new JsonFileStore<Dictionary<string, MoodAnalysis>>("moods.json", dataDirectory);
        }

        public Dictionary<string, MoodAnalysis> LoadAll() => _store.Load();

        public void SaveAll(Dictionary<string, MoodAnalysis> moods) => _store.Save(moods);
    }
}
