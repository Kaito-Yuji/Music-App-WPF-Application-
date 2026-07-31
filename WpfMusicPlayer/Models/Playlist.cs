using System.Collections.ObjectModel;
using System.Collections.Specialized;
using Microsoft.Toolkit.Mvvm.ComponentModel;

namespace WpfMusicPlayer.Models
{
    public class Playlist : ObservableObject
    {
        private string _name = string.Empty;
        private string _description = string.Empty;
        private byte[]? _coverImage;

        public Playlist()
        {
            // Keep the computed summary properties in sync with the song list
            Songs.CollectionChanged += OnSongsChanged;
        }

        public string Id { get; set; } = Guid.NewGuid().ToString();

        public string Name
        {
            get => _name;
            set => SetProperty(ref _name, value);
        }

        public string Description
        {
            get => _description;
            set => SetProperty(ref _description, value);
        }

        public byte[]? CoverImage
        {
            get => _coverImage;
            set => SetProperty(ref _coverImage, value);
        }

        public ObservableCollection<Song> Songs { get; } = new ObservableCollection<Song>();
        public DateTime CreatedDate { get; set; } = DateTime.Now;

        public int SongCount => Songs.Count;
        public TimeSpan TotalDuration => TimeSpan.FromSeconds(Songs.Sum(s => s.Duration.TotalSeconds));
        public string TotalDurationText => TotalDuration.ToString(@"hh\:mm\:ss");

        private void OnSongsChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            OnPropertyChanged(nameof(SongCount));
            OnPropertyChanged(nameof(TotalDuration));
            OnPropertyChanged(nameof(TotalDurationText));
        }
    }
}
