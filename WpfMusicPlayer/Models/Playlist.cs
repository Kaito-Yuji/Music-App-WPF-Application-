using System.Collections.ObjectModel;

namespace WpfMusicPlayer.Models
{
    public class Playlist
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public byte[]? CoverImage { get; set; }
        public ObservableCollection<Song> Songs { get; set; } = new ObservableCollection<Song>();
        public DateTime CreatedDate { get; set; } = DateTime.Now;

        public int SongCount => Songs.Count;
        public TimeSpan TotalDuration => TimeSpan.FromSeconds(Songs.Sum(s => s.Duration.TotalSeconds));
        public string TotalDurationText => TotalDuration.ToString(@"hh\:mm\:ss");
    }
}
