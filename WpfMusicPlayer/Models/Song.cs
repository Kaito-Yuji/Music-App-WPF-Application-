using System;

namespace WpfMusicPlayer.Models
{
    public class Song : IEquatable<Song>
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

        public bool Equals(Song? other)
        {
            if (other is null) return false;
            if (ReferenceEquals(this, other)) return true;
            
            // Compare by ID first, then by file path if IDs don't match
            return Id == other.Id || FilePath == other.FilePath;
        }

        public override bool Equals(object? obj)
        {
            return Equals(obj as Song);
        }

        public override int GetHashCode()
        {
            // Use ID for hash code, fall back to file path if ID is empty
            return !string.IsNullOrEmpty(Id) ? Id.GetHashCode() : FilePath.GetHashCode();
        }

        public static bool operator ==(Song? left, Song? right)
        {
            return EqualityComparer<Song>.Default.Equals(left, right);
        }

        public static bool operator !=(Song? left, Song? right)
        {
            return !(left == right);
        }
    }
}
