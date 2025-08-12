using System;

namespace WpfMusicPlayer.Models
{
    public class Song
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Title { get; set; } = string.Empty;
        public string Artist { get; set; } = string.Empty;
        public string Album { get; set; } = string.Empty;
        public string FilePath { get; set; } = string.Empty;
        public TimeSpan Duration { get; set; }
        public string Genre { get; set; } = string.Empty;
        public int Year { get; set; }
        public byte[]? AlbumArt { get; set; }

        public string DisplayText => $"{Artist} - {Title}";
        public string DurationText => Duration.ToString(@"mm\:ss");
    }
}
