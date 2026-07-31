using System.Windows;
using WpfMusicPlayer.ViewModels;

namespace WpfMusicPlayer
{
    public partial class ListeningStatsWindow : Window
    {
        public ListeningStatsWindow(ListeningStatsViewModel viewModel)
        {
            InitializeComponent();
            DataContext = viewModel;
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e) => Close();
    }
}
