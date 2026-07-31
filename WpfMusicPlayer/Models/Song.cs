using System;
using Microsoft.Toolkit.Mvvm.ComponentModel;

namespace WpfMusicPlayer.Models
{
    public class Song : ObservableObject, IEquatable<Song>
    {
        private string _title = string.Empty;
        private string _artist = string.Empty;
        private string _album = string.Empty;
        private string _genre = string.Empty;
        private int _year;
        private TimeSpan _duration;
        private byte[]? _albumArt;
        private int _viewCount;
        private Mood _mood = Mood.Unknown;
        private string _moodDetails = "Not analysed yet";

        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string FilePath { get; set; } = string.Empty;

        public string Title
        {
            get => _title;
            set
            {
                if (SetProperty(ref _title, value))
                    OnPropertyChanged(nameof(DisplayText));
            }
        }

        public string Artist
        {
            get => _artist;
            set
            {
                if (SetProperty(ref _artist, value))
                    OnPropertyChanged(nameof(DisplayText));
            }
        }

        public string Album
        {
            get => _album;
            set => SetProperty(ref _album, value);
        }

        public string Genre
        {
            get => _genre;
            set => SetProperty(ref _genre, value);
        }

        public int Year
        {
            get => _year;
            set => SetProperty(ref _year, value);
        }

        public TimeSpan Duration
        {
            get => _duration;
            set
            {
                if (SetProperty(ref _duration, value))
                    OnPropertyChanged(nameof(DurationText));
            }
        }

        public byte[]? AlbumArt
        {
            get => _albumArt;
            set => SetProperty(ref _albumArt, value);
        }

        // View counting
        public int ViewCount
        {
            get => _viewCount;
            set => SetProperty(ref _viewCount, value);
        }

        /// <summary>Emotional character estimated from the audio. Unknown until analysed.</summary>
        public Mood Mood
        {
            get => _mood;
            set => SetProperty(ref _mood, value);
        }

        /// <summary>The measurements behind <see cref="Mood"/>, shown as a tooltip.</summary>
        public string MoodDetails
        {
            get => _moodDetails;
            set => SetProperty(ref _moodDetails, value);
        }

        public string DisplayText => $"{Artist} - {Title}";
        public string DurationText => Duration.ToString(@"mm\:ss");

        /// <summary>
        /// The value that decides song identity. The file path is the natural key - it is what
        /// the stats files are keyed by - and the generated Id is only a fallback for songs that
        /// have no path yet. Equals and GetHashCode both use this, so they can never disagree.
        /// </summary>
        private string IdentityKey =>
            string.IsNullOrEmpty(FilePath) ? Id : FilePath.ToLowerInvariant();

        public bool Equals(Song? other)
        {
            if (other is null) return false;
            if (ReferenceEquals(this, other)) return true;

            return IdentityKey == other.IdentityKey;
        }

        public override bool Equals(object? obj)
        {
            return Equals(obj as Song);
        }

        public override int GetHashCode()
        {
            return IdentityKey.GetHashCode();
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
