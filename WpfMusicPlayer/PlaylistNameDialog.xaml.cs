using Microsoft.Win32;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Imaging;

namespace WpfMusicPlayer
{
    public partial class PlaylistNameDialog : Window
    {
        public string PlaylistName { get; set; } = string.Empty;
        public string PlaylistDescription { get; set; } = string.Empty;
        public byte[]? PlaylistCover { get; private set; }

        public PlaylistNameDialog()
        {
            InitializeComponent();
            
            // Setup character count updates
            NameTextBox.TextChanged += (s, e) => UpdateCharCount();
            DescriptionTextBox.TextChanged += (s, e) => UpdateCharCount();
            
            Loaded += PlaylistNameDialog_Loaded;
        }

        private void PlaylistNameDialog_Loaded(object sender, RoutedEventArgs e)
        {
            // Set initial values if provided
            NameTextBox.Text = PlaylistName;
            DescriptionTextBox.Text = PlaylistDescription;
            NameTextBox.Focus();
            UpdateCharCount();
        }

        private void UpdateCharCount()
        {
            NameCharCountText.Text = $"{NameTextBox.Text.Length}/50";
            DescCharCountText.Text = $"{DescriptionTextBox.Text.Length}/200";
        }

        private void CoverBorder_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            var openFileDialog = new OpenFileDialog
            {
                Title = "Select Playlist Cover Image",
                Filter = "Image files (*.jpg, *.jpeg, *.png, *.bmp, *.gif)|*.jpg;*.jpeg;*.png;*.bmp;*.gif",
                RestoreDirectory = true
            };

            if (openFileDialog.ShowDialog() == true)
            {
                try
                {
                    // Load and display the image
                    var bitmap = new BitmapImage();
                    bitmap.BeginInit();
                    bitmap.UriSource = new Uri(openFileDialog.FileName);
                    bitmap.DecodePixelWidth = 150; // Resize for performance
                    bitmap.EndInit();
                    
                    CoverImage.Source = bitmap;
                    CoverImage.Visibility = Visibility.Visible;
                    
                    // Convert to byte array for storage
                    PlaylistCover = File.ReadAllBytes(openFileDialog.FileName);
                    
                    // Hide the placeholder text
                    foreach (var child in ((Grid)CoverBorder.Child).Children)
                    {
                        if (child is StackPanel stackPanel)
                        {
                            stackPanel.Visibility = Visibility.Collapsed;
                            break;
                        }
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error loading image: {ex.Message}", "Error", 
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
        }

        private void OkButton_Click(object sender, RoutedEventArgs e)
        {
            if (!string.IsNullOrWhiteSpace(NameTextBox.Text))
            {
                PlaylistName = NameTextBox.Text.Trim();
                PlaylistDescription = DescriptionTextBox.Text.Trim();
                DialogResult = true;
            }
            else
            {
                MessageBox.Show("Please enter a playlist name.", "Error", 
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                NameTextBox.Focus();
            }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
        }

        private void NameTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                OkButton_Click(sender, e);
            }
        }
    }
}
