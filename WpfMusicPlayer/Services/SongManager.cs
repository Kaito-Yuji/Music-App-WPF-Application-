using System;
using System.Collections.ObjectModel;
using System.Linq;
using WpfMusicPlayer.Models;

namespace WpfMusicPlayer.Services
{
    public class SongManager
    {
        private readonly PlayerService _playerService;

        public SongManager(PlayerService playerService)
        {
            _playerService = playerService ?? throw new ArgumentNullException(nameof(playerService));
        }

        /// <summary>
        /// Plays the exact song that was clicked, ensuring no mismatch
        /// </summary>
        public void PlayExactSong(Song clickedSong, ObservableCollection<Song> songCollection)
        {
            // First create an exact copy of the collection to ensure index integrity
            var songs = new ObservableCollection<Song>(songCollection);
            
            // Find the EXACT song by ID
            var exactSongIndex = -1;
            for (int i = 0; i < songs.Count; i++)
            {
                if (songs[i].Id == clickedSong.Id)
                {
                    exactSongIndex = i;
                    break;
                }
            }

            if (exactSongIndex >= 0)
            {
                // Set the queue with the exact index
                _playerService.SetQueue(songs, exactSongIndex);
                
                // Double-check that we're playing the right song
                var songToPlay = songs[exactSongIndex];
                
                // Direct load and play - bypass any index calculation issues
                try
                {
                    _playerService.PlaySongAtIndex(exactSongIndex);
                    
                    // Debug verification
                    System.Diagnostics.Debug.WriteLine($"Playing song: {songToPlay.Title} at index {exactSongIndex}");
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error playing song: {ex.Message}");
                }
            }
        }

        /// <summary>
        /// Play the next song, ensuring proper index calculation
        /// </summary>
        public void PlayNextSong()
        {
            var currentMode = _playerService.PlayMode;
            var currentIndex = _playerService.CurrentIndex;
            var queue = _playerService.Queue;
            
            // Calculate next index based on play mode
            int nextIndex;
            
            if (queue.Count == 0)
            {
                return; // Nothing to play
            }
            
            switch (currentMode)
            {
                case PlayMode.RepeatOne:
                    // Play the same song again
                    nextIndex = currentIndex;
                    break;
                    
                case PlayMode.Shuffle:
                    // Play random song
                    var random = new Random();
                    nextIndex = random.Next(queue.Count);
                    break;
                    
                case PlayMode.RepeatAll:
                    // Go to next song, wrap around if at end
                    nextIndex = (currentIndex + 1) % queue.Count;
                    break;
                    
                default: // Normal mode
                    // Go to next song if available
                    nextIndex = currentIndex + 1 < queue.Count ? currentIndex + 1 : -1;
                    break;
            }
            
            // Play the song if we have a valid index
            if (nextIndex >= 0)
            {
                // Direct call to ensure index integrity
                _playerService.PlaySongAtIndex(nextIndex);
                
                // Debug verification
                System.Diagnostics.Debug.WriteLine($"Playing next song at index {nextIndex}");
            }
        }

        /// <summary>
        /// Play the previous song, ensuring proper index calculation
        /// </summary>
        public void PlayPreviousSong()
        {
            var currentMode = _playerService.PlayMode;
            var currentIndex = _playerService.CurrentIndex;
            var queue = _playerService.Queue;
            
            // Calculate previous index based on play mode
            int prevIndex;
            
            if (queue.Count == 0)
            {
                return; // Nothing to play
            }
            
            switch (currentMode)
            {
                case PlayMode.RepeatOne:
                    // Play the same song again
                    prevIndex = currentIndex;
                    break;
                    
                case PlayMode.Shuffle:
                    // Play random song
                    var random = new Random();
                    prevIndex = random.Next(queue.Count);
                    break;
                    
                case PlayMode.RepeatAll:
                    // Go to previous song, wrap around if at beginning
                    prevIndex = currentIndex - 1 >= 0 ? currentIndex - 1 : queue.Count - 1;
                    break;
                    
                default: // Normal mode
                    // Go to previous song if available
                    prevIndex = currentIndex - 1 >= 0 ? currentIndex - 1 : -1;
                    break;
            }
            
            // Play the song if we have a valid index
            if (prevIndex >= 0)
            {
                // Direct call to ensure index integrity
                _playerService.PlaySongAtIndex(prevIndex);
                
                // Debug verification
                System.Diagnostics.Debug.WriteLine($"Playing previous song at index {prevIndex}");
            }
        }
    }
}
