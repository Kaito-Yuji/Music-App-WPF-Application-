using System;
using System.ComponentModel;
using System.IO;
using System.Windows;
using System.Windows.Media.Imaging;
using Microsoft.Win32;
using WpfMusicPlayer.Models;
using WpfMusicPlayer.Services;

namespace WpfMusicPlayer
{
    public partial class SongEditDialog : Window
    {
        private readonly Song _originalSong;
        private readonly Song _editedSong;

        public SongEditDialog(Song song)
        {
            InitializeComponent();

            _originalSong = song;
            _editedSong = CloneSong(song);

            DataContext = _editedSong;
        }

        public Song EditedSong => _editedSong;

        public bool WasModified { get; private set; }

        /// <summary>True when the listener picked or cleared the artwork in this dialog.</summary>
        public bool AlbumArtChanged { get; private set; }

        /// <summary>
        /// Full-resolution artwork chosen here. The song itself only carries a thumbnail for
        /// display, so this is what must be written into the file's tags.
        /// </summary>
        public byte[]? RawAlbumArt { get; private set; }

        private static Song CloneSong(Song original) => new()
        {
            Id = original.Id,
            Title = original.Title,
            Artist = original.Artist,
            Album = original.Album,
            FilePath = original.FilePath,
            Duration = original.Duration,
            Genre = original.Genre,
            Year = original.Year,
            AlbumArt = original.AlbumArt?.ToArray()
        };

        private void ChangeImageButton_Click(object sender, RoutedEventArgs e)
        {
            var openFileDialog = new OpenFileDialog
            {
                Title = "Select Album Art Image",
                Filter = "Image Files|*.jpg;*.jpeg;*.png;*.bmp;*.gif|" +
                         "JPEG Files (*.jpg;*.jpeg)|*.jpg;*.jpeg|" +
                         "PNG Files (*.png)|*.png|" +
                         "Bitmap Files (*.bmp)|*.bmp|" +
                         "GIF Files (*.gif)|*.gif|" +
                         "All Files (*.*)|*.*",
                FilterIndex = 1
            };

            if (openFileDialog.ShowDialog() != true)
                return;

            try
            {
                var imageBytes = File.ReadAllBytes(openFileDialog.FileName);

                // Decode it once to make sure it really is an image before accepting it
                using var stream = new MemoryStream(imageBytes);
                var bitmap = new BitmapImage();
                bitmap.BeginInit();
                bitmap.StreamSource = stream;
                bitmap.CacheOption = BitmapCacheOption.OnLoad;
                bitmap.EndInit();

                RawAlbumArt = imageBytes;
                AlbumArtChanged = true;
                WasModified = true;

                // The preview shows the same thumbnail the library will hold
                _editedSong.AlbumArt = ImageUtils.CreateThumbnail(imageBytes);
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, $"Failed to load image: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void RemoveImageButton_Click(object sender, RoutedEventArgs e)
        {
            var result = MessageBox.Show(this, "Are you sure you want to remove the album art?",
                "Confirm Remove", MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (result != MessageBoxResult.Yes)
                return;

            _editedSong.AlbumArt = null;
            RawAlbumArt = null;
            AlbumArtChanged = true;
            WasModified = true;
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(_editedSong.Title))
            {
                MessageBox.Show(this, "Title is required.", "Validation Error",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                TitleTextBox.Focus();
                return;
            }

            if (string.IsNullOrWhiteSpace(_editedSong.Artist))
            {
                MessageBox.Show(this, "Artist is required.", "Validation Error",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                ArtistTextBox.Focus();
                return;
            }

            if (!string.IsNullOrWhiteSpace(YearTextBox.Text) && !int.TryParse(YearTextBox.Text, out _))
            {
                MessageBox.Show(this, "Please enter a valid year.", "Validation Error",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                YearTextBox.Focus();
                return;
            }

            if (HasTextChanges())
                WasModified = true;

            DialogResult = true;
        }

        private bool HasTextChanges() =>
            _editedSong.Title != _originalSong.Title ||
            _editedSong.Artist != _originalSong.Artist ||
            _editedSong.Album != _originalSong.Album ||
            _editedSong.Genre != _originalSong.Genre ||
            _editedSong.Year != _originalSong.Year;

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            var hasChanges = HasTextChanges() || AlbumArtChanged;

            if (hasChanges && DialogResult != true)
            {
                var result = MessageBox.Show(this,
                    "You have unsaved changes. Are you sure you want to close?",
                    "Unsaved Changes", MessageBoxButton.YesNo, MessageBoxImage.Question);

                if (result == MessageBoxResult.No)
                {
                    e.Cancel = true;
                    return;
                }
            }

            base.OnClosing(e);
        }
    }
}
