using WpfMusicPlayer.Models;

namespace WpfMusicPlayer.Interfaces
{
    public interface IAppStateRepository
    {
        /// <summary>The remembered session. Mutate it, then call <see cref="Save"/>.</summary>
        AppState State { get; }

        void Save();
    }
}
