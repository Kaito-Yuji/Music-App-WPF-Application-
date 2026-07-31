using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using WpfMusicPlayer.Models;
using WpfMusicPlayer.ViewModels;

namespace WpfMusicPlayer
{
    /// <summary>
    /// View-only concerns live here: column sizing, slider dragging, drag-and-drop and the
    /// keyboard shortcut that must not fire while the search box has focus. Everything else is
    /// bound to <see cref="MainViewModel"/>.
    /// </summary>
    public partial class MainWindow : Window
    {
        private readonly MainViewModel _viewModel;
        private BindingBase? _positionBinding;
        private bool _isDraggingSlider;

        public MainWindow(MainViewModel viewModel)
        {
            InitializeComponent();

            _viewModel = viewModel;
            DataContext = viewModel;

            Loaded += MainWindow_Loaded;
            SizeChanged += (_, _) => UpdateColumnWidths();
        }

        private async void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            UpdateColumnWidths();

            // Remembered so it can be reattached after a drag; while dragging, the position
            // updates would otherwise yank the thumb back every 100 ms
            _positionBinding = BindingOperations.GetBinding(ProgressSlider, Slider.ValueProperty);

            await _viewModel.InitializeAsync();
        }

        #region Responsive columns

        private void UpdateColumnWidths()
        {
            if (SongsListView?.View is not GridView gridView || gridView.Columns.Count < 9)
                return;

            var availableWidth = SongsListView.ActualWidth - 40; // scrollbar and padding
            if (availableWidth <= 0)
                return;

            const double numberWidth = 40;
            const double coverWidth = 80;
            const double durationWidth = 80;
            const double playsWidth = 70;
            const double moodWidth = 110;
            const double actionsWidth = 150;

            var remainingWidth = availableWidth
                                 - numberWidth - coverWidth - durationWidth - playsWidth - moodWidth - actionsWidth;

            if (remainingWidth <= 300)
                return;

            gridView.Columns[0].Width = numberWidth;
            gridView.Columns[1].Width = coverWidth;
            gridView.Columns[2].Width = Math.Max(150, remainingWidth * 0.4);
            gridView.Columns[3].Width = Math.Max(120, remainingWidth * 0.3);
            gridView.Columns[4].Width = Math.Max(120, remainingWidth * 0.3);
            gridView.Columns[5].Width = durationWidth;
            gridView.Columns[6].Width = playsWidth;
            gridView.Columns[7].Width = moodWidth;
            gridView.Columns[8].Width = actionsWidth;
        }

        #endregion

        #region Seeking

        private void ProgressSlider_DragStarted(object sender, DragStartedEventArgs e)
        {
            _isDraggingSlider = true;
            BindingOperations.ClearBinding(ProgressSlider, Slider.ValueProperty);
        }

        private void ProgressSlider_DragCompleted(object sender, DragCompletedEventArgs e)
        {
            SeekToSliderValue();

            if (_positionBinding != null)
                BindingOperations.SetBinding(ProgressSlider, Slider.ValueProperty, _positionBinding);

            _isDraggingSlider = false;
        }

        private void ProgressSlider_PreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            // Clicking the track moves the thumb straight there (IsMoveToPointEnabled)
            if (!_isDraggingSlider)
                SeekToSliderValue();
        }

        private void SeekToSliderValue()
        {
            _viewModel.Playback.Seek(TimeSpan.FromSeconds(ProgressSlider.Value));
        }

        #endregion

        #region Double-click activation

        // These are wired as events rather than MouseBinding + CommandParameter: input bindings
        // live outside the logical tree, so an ElementName binding inside one silently resolves
        // to null and the command does nothing at all.

        private void SongsListView_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (!IsItemActivation<ListViewItem>(e))
                return;

            if (SongsListView.SelectedItem is Song song)
                _viewModel.PlaySongCommand.Execute(song);
        }

        private void QueueListBox_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (!IsItemActivation<ListBoxItem>(e))
                return;

            if (QueueListBox.SelectedItem is Song song)
                _viewModel.PlayFromQueueCommand.Execute(song);
        }

        private void PlaylistsListBox_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (!IsItemActivation<ListBoxItem>(e))
                return;

            if (PlaylistsListBox.SelectedItem is Playlist playlist)
                _viewModel.OpenPlaylistCommand.Execute(playlist);
        }

        /// <summary>
        /// True when the double-click landed on a row rather than on empty space below the list
        /// or on one of the buttons a row carries.
        /// </summary>
        internal static bool IsItemActivation<TContainer>(MouseButtonEventArgs e)
            where TContainer : DependencyObject
        {
            if (FindAncestor<ButtonBase>(e.OriginalSource) != null)
                return false;

            return FindAncestor<TContainer>(e.OriginalSource) != null;
        }

        private static T? FindAncestor<T>(object? source) where T : DependencyObject
        {
            var current = source as DependencyObject;

            while (current != null)
            {
                if (current is T match)
                    return match;

                // Text runs inside a template are content elements, not visuals
                current = current is Visual
                    ? VisualTreeHelper.GetParent(current)
                    : LogicalTreeHelper.GetParent(current);
            }

            return null;
        }

        #endregion

        #region Drag and drop

        private void Window_DragOver(object sender, DragEventArgs e)
        {
            e.Effects = e.Data.GetDataPresent(DataFormats.FileDrop) ? DragDropEffects.Copy : DragDropEffects.None;
            e.Handled = true;
        }

        private async void Window_Drop(object sender, DragEventArgs e)
        {
            if (e.Data.GetData(DataFormats.FileDrop) is not string[] paths || paths.Length == 0)
                return;

            await _viewModel.AddDroppedPathsAsync(paths);
        }

        #endregion

        private void Window_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            // Space toggles playback, but only when the listener is not typing
            if (e.Key != Key.Space || Keyboard.FocusedElement is TextBox)
                return;

            _viewModel.PlayPauseCommand.Execute(null);
            e.Handled = true;
        }

        protected override void OnClosed(EventArgs e)
        {
            _viewModel.Dispose();
            base.OnClosed(e);
        }
    }
}
