using Microsoft.Win32;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Media.Imaging;
using WpfMusicPlayer.Models;

namespace WpfMusicPlayer
{
    public partial class SongEditDialog : Window, INotifyPropertyChanged
    {
        private Song _originalSong;
        private Song _editedSong;

        public Song EditedSong => _editedSong;
        public bool WasModified { get; private set; }

        public SongEditDialog(Song song)
        {
            InitializeComponent();
            
            _originalSong = song;
            _editedSong = CloneSong(song);
            
            DataContext = _editedSong;
            UpdateAlbumArtPreview();
        }

        private Song CloneSong(Song original)
        {
            return new Song
            {
                Id = original.Id,
                Title = original.Title,
                Artist = original.Artist,
                Album = original.Album,
                FilePath = original.FilePath,
                Duration = original.Duration,
                Genre = original.Genre,
                Year = original.Year,
                AlbumArt = original.AlbumArt?.ToArray() // Create a copy of the byte array
            };
        }

        private void UpdateAlbumArtPreview()
        {
            // Trigger property change to update the image binding
            OnPropertyChanged(nameof(_editedSong.AlbumArt));
        }

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

            if (openFileDialog.ShowDialog() == true)
            {
                try
                {
                    // Load and convert image to byte array
                    var imageBytes = File.ReadAllBytes(openFileDialog.FileName);
                    
                    // Validate the image by trying to load it
                    using var stream = new MemoryStream(imageBytes);
                    var bitmap = new BitmapImage();
                    bitmap.BeginInit();
                    bitmap.StreamSource = stream;
                    bitmap.CacheOption = BitmapCacheOption.OnLoad;
                    bitmap.EndInit();

                    // If we get here, the image is valid
                    _editedSong.AlbumArt = imageBytes;
                    UpdateAlbumArtPreview();
                    WasModified = true;
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Failed to load image: {ex.Message}", "Error", 
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void RemoveImageButton_Click(object sender, RoutedEventArgs e)
        {
            var result = MessageBox.Show("Are you sure you want to remove the album art?", 
                "Confirm Remove", MessageBoxButton.YesNo, MessageBoxImage.Question);
            
            if (result == MessageBoxResult.Yes)
            {
                _editedSong.AlbumArt = null;
                UpdateAlbumArtPreview();
                WasModified = true;
            }
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            // Validate required fields
            if (string.IsNullOrWhiteSpace(_editedSong.Title))
            {
                MessageBox.Show("Title is required.", "Validation Error", 
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                TitleTextBox.Focus();
                return;
            }

            if (string.IsNullOrWhiteSpace(_editedSong.Artist))
            {
                MessageBox.Show("Artist is required.", "Validation Error", 
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                ArtistTextBox.Focus();
                return;
            }

            // Parse year if provided
            if (!string.IsNullOrWhiteSpace(YearTextBox.Text))
            {
                if (int.TryParse(YearTextBox.Text, out int year))
                {
                    _editedSong.Year = year;
                }
                else
                {
                    MessageBox.Show("Please enter a valid year.", "Validation Error", 
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    YearTextBox.Focus();
                    return;
                }
            }

            // Check if any changes were made
            if (HasChanges())
            {
                WasModified = true;
            }

            DialogResult = true;
            Close();
        }

        private bool HasChanges()
        {
            return _editedSong.Title != _originalSong.Title ||
                   _editedSong.Artist != _originalSong.Artist ||
                   _editedSong.Album != _originalSong.Album ||
                   _editedSong.Genre != _originalSong.Genre ||
                   _editedSong.Year != _originalSong.Year ||
                   !AreByteArraysEqual(_editedSong.AlbumArt, _originalSong.AlbumArt);
        }

        private bool AreByteArraysEqual(byte[]? a, byte[]? b)
        {
            if (a == null && b == null) return true;
            if (a == null || b == null) return false;
            if (a.Length != b.Length) return false;
            
            for (int i = 0; i < a.Length; i++)
            {
                if (a[i] != b[i]) return false;
            }
            return true;
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            if (HasChanges() && DialogResult != true)
            {
                var result = MessageBox.Show("You have unsaved changes. Are you sure you want to close?", 
                    "Unsaved Changes", MessageBoxButton.YesNo, MessageBoxImage.Question);
                
                if (result == MessageBoxResult.No)
                {
                    e.Cancel = true;
                    return;
                }
            }
            
            base.OnClosing(e);
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
