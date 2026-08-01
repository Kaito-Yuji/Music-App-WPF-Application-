using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using Microsoft.Toolkit.Mvvm.ComponentModel;
using Microsoft.Toolkit.Mvvm.Input;
using WpfMusicPlayer.Interfaces;
using WpfMusicPlayer.Models;
using WpfMusicPlayer.Services;

namespace WpfMusicPlayer.ViewModels
{
    public enum KaraokeState
    {
        /// <summary>Nothing is playing, or separation is not possible on this machine.</summary>
        Unavailable,

        /// <summary>Playing a track whose stems have not been produced yet.</summary>
        NeedsSeparation,

        /// <summary>Stems are on disk; switching is instant.</summary>
        Ready,

        /// <summary>Currently playing the instrumental stem.</summary>
        Active
    }

    public class MainViewModel : ObservableObject, IDisposable
    {
        private readonly AppServices _services;
        private readonly IDialogService _dialogs;

        private CancellationTokenSource? _separationCts;
        private string _searchText = string.Empty;
        private string _statusText = "Ready";
        private Song? _selectedSong;
        private bool _isBusy;
        private bool _isOverlayVisible;
        private string _loadingText = "Processing audio...";
        private string _loadingDetails = string.Empty;
        private int _loadingProgress;
        private bool _isLoadingProgressIndeterminate = true;
        private KaraokeState _karaokeState = KaraokeState.Unavailable;

        public MainViewModel(AppServices services, IDialogService dialogs)
        {
            _services = services;
            _dialogs = dialogs;

            ScanMusicCommand = new AsyncRelayCommand(ScanMusicAsync);
            CreatePlaylistCommand = new RelayCommand(CreatePlaylist);
            OpenStatisticsCommand = new RelayCommand(_dialogs.ShowStatistics);
            OpenPlaylistCommand = new RelayCommand<Playlist>(OpenPlaylist);
            DeletePlaylistCommand = new RelayCommand<Playlist>(DeletePlaylist);
            AddToPlaylistCommand = new RelayCommand<Song>(AddToPlaylist);
            EditSongCommand = new RelayCommand<Song>(EditSong);
            PlaySongCommand = new RelayCommand<Song>(PlaySong);
            PlayFromQueueCommand = new RelayCommand<Song>(song =>
            {
                if (song != null) Invoke(() => Playback.PlaySong(song));
            });
            PlayPauseCommand = new RelayCommand(PlayPause);
            NextCommand = new RelayCommand(() => Invoke(Playback.PlayNext));
            PreviousCommand = new RelayCommand(() => Invoke(Playback.PlayPrevious));
            ToggleShuffleCommand = new RelayCommand(() => Playback.IsShuffleOn = !Playback.IsShuffleOn);
            CycleRepeatCommand = new RelayCommand(CycleRepeat);
            RemoveFromQueueCommand = new RelayCommand<Song>(song =>
            {
                if (song != null) Playback.RemoveFromQueue(song);
            });
            KaraokeCommand = new AsyncRelayCommand(ToggleKaraokeAsync, () => !IsBusy);
            CancelJobCommand = new RelayCommand(CancelJob, () => IsBusy);
            BatchSeparateCommand = new AsyncRelayCommand(BatchSeparateAsync, () => !IsBusy);
            ExportStemsCommand = new AsyncRelayCommand(ExportStemsAsync, () => !IsBusy);
            AnalyzeMoodCommand = new AsyncRelayCommand(AnalyzeMoodAsync, () => !IsBusy);
            AnalyzeAllMoodsCommand = new AsyncRelayCommand(AnalyzeAllMoodsAsync, () => !IsBusy);
            CreateMoodPlaylistsCommand = new RelayCommand(CreateMoodPlaylists);

            WireUpServices();
        }

        #region Exposed services and collections

        public IPlaybackService Playback => _services.Playback;
        public IKaraokeService Karaoke => _services.Karaoke;
        public IMoodAnalyzerService MoodAnalyzer => _services.Mood;

        public ObservableCollection<Song> DisplayedSongs { get; } = new();
        public ObservableCollection<Playlist> Playlists => _services.Playlists.Playlists;
        public ObservableCollection<Song> Queue => _services.Playback.Queue;

        #endregion

        #region Bindable state

        public string SearchText
        {
            get => _searchText;
            set
            {
                if (!SetProperty(ref _searchText, value))
                    return;

                ApplySearch();
            }
        }

        public string StatusText
        {
            get => _statusText;
            set => SetProperty(ref _statusText, value);
        }

        public Song? SelectedSong
        {
            get => _selectedSong;
            set => SetProperty(ref _selectedSong, value);
        }

        public double Volume
        {
            get => Playback.Volume;
            set
            {
                if (Math.Abs(Playback.Volume - (float)value) < 0.0001f)
                    return;

                Playback.Volume = (float)value;
                OnPropertyChanged();
            }
        }

        public bool IsBusy
        {
            get => _isBusy;
            private set
            {
                if (!SetProperty(ref _isBusy, value))
                    return;

                KaraokeCommand.NotifyCanExecuteChanged();
                BatchSeparateCommand.NotifyCanExecuteChanged();
                ExportStemsCommand.NotifyCanExecuteChanged();
                AnalyzeMoodCommand.NotifyCanExecuteChanged();
                AnalyzeAllMoodsCommand.NotifyCanExecuteChanged();
                CancelJobCommand.NotifyCanExecuteChanged();
            }
        }

        public bool IsOverlayVisible
        {
            get => _isOverlayVisible;
            private set => SetProperty(ref _isOverlayVisible, value);
        }

        public string LoadingText
        {
            get => _loadingText;
            private set => SetProperty(ref _loadingText, value);
        }

        public string LoadingDetails
        {
            get => _loadingDetails;
            private set => SetProperty(ref _loadingDetails, value);
        }

        public int LoadingProgress
        {
            get => _loadingProgress;
            private set => SetProperty(ref _loadingProgress, value);
        }

        public bool IsLoadingProgressIndeterminate
        {
            get => _isLoadingProgressIndeterminate;
            private set => SetProperty(ref _isLoadingProgressIndeterminate, value);
        }

        public KaraokeState KaraokeState
        {
            get => _karaokeState;
            private set => SetProperty(ref _karaokeState, value);
        }

        #endregion

        #region Commands

        public AsyncRelayCommand ScanMusicCommand { get; }
        public RelayCommand CreatePlaylistCommand { get; }
        public RelayCommand OpenStatisticsCommand { get; }
        public RelayCommand<Playlist> OpenPlaylistCommand { get; }
        public RelayCommand<Playlist> DeletePlaylistCommand { get; }
        public RelayCommand<Song> AddToPlaylistCommand { get; }
        public RelayCommand<Song> EditSongCommand { get; }
        public RelayCommand<Song> PlaySongCommand { get; }
        public RelayCommand<Song> PlayFromQueueCommand { get; }
        public RelayCommand PlayPauseCommand { get; }
        public RelayCommand NextCommand { get; }
        public RelayCommand PreviousCommand { get; }
        public RelayCommand ToggleShuffleCommand { get; }
        public RelayCommand CycleRepeatCommand { get; }
        public RelayCommand<Song> RemoveFromQueueCommand { get; }
        public AsyncRelayCommand KaraokeCommand { get; }
        public RelayCommand CancelJobCommand { get; }
        public AsyncRelayCommand BatchSeparateCommand { get; }
        public AsyncRelayCommand ExportStemsCommand { get; }
        public AsyncRelayCommand AnalyzeMoodCommand { get; }
        public AsyncRelayCommand AnalyzeAllMoodsCommand { get; }
        public RelayCommand CreateMoodPlaylistsCommand { get; }

        #endregion

        #region Startup and shutdown

        private void WireUpServices()
        {
            Playback.TrackStarted += (_, song) => _services.Stats.OnSongStarted(song);

            Playback.PositionChanged += (_, _) =>
                _services.Stats.OnPositionUpdate(Playback.CurrentSong, Playback.CurrentPosition, Playback.TotalDuration);

            Playback.CurrentSongChanged += (_, _) => UpdateKaraokeState();

            _services.Karaoke.PropertyChanged += (_, _) => UpdateKaraokeState();
            _services.Karaoke.Progress += OnSeparationProgress;

            _services.Stats.PlayCounted += (_, filePath) => RunOnUi(() =>
            {
                var song = _services.Library.Songs.FirstOrDefault(
                    s => string.Equals(s.FilePath, filePath, StringComparison.OrdinalIgnoreCase));

                // Song implements INotifyPropertyChanged, so the grid updates on its own
                if (song != null)
                    song.ViewCount++;
            });
        }

        /// <summary>Loads the remembered library and playback session.</summary>
        public async Task InitializeAsync()
        {
            var state = _services.State.State;

            Playback.Volume = state.Volume;
            Playback.IsShuffleOn = state.IsShuffleOn;
            Playback.RepeatMode = state.RepeatMode;
            OnPropertyChanged(nameof(Volume));

            if (string.IsNullOrWhiteSpace(state.MusicFolderPath) || !System.IO.Directory.Exists(state.MusicFolderPath))
                return;

            StatusText = "Loading saved music library...";
            await ScanFolderAsync(state.MusicFolderPath);
            RestoreQueue();
        }

        private void RestoreQueue()
        {
            var state = _services.State.State;
            if (state.QueueFilePaths.Count == 0)
                return;

            var byPath = _services.Library.Songs.ToDictionary(
                s => s.FilePath, StringComparer.OrdinalIgnoreCase);

            var queue = state.QueueFilePaths
                .Where(byPath.ContainsKey)
                .Select(path => byPath[path])
                .ToList();

            if (queue.Count == 0)
                return;

            var index = Math.Max(0, Math.Min(state.CurrentIndex, queue.Count - 1));
            Playback.SetQueue(queue, index);
            Playback.PrepareAt(index, TimeSpan.FromSeconds(state.PositionSeconds));

            StatusText = $"Restored your last session - {queue.Count} songs in the queue";
        }

        public void SaveState()
        {
            var state = _services.State.State;

            state.Volume = Playback.Volume;
            state.IsShuffleOn = Playback.IsShuffleOn;
            state.RepeatMode = Playback.RepeatMode;
            state.QueueFilePaths = Queue.Select(s => s.FilePath).ToList();
            state.CurrentIndex = Playback.CurrentIndex;
            state.PositionSeconds = Playback.CurrentPosition.TotalSeconds;

            _services.State.Save();
        }

        public void Dispose()
        {
            _separationCts?.Cancel();
            _separationCts?.Dispose();

            _services.Stats.OnSongStopped();
            SaveState();
        }

        #endregion

        #region Library

        private async Task ScanMusicAsync()
        {
            var folder = _dialogs.PickFolder("Select Music Folder");
            if (string.IsNullOrEmpty(folder))
                return;

            await ScanFolderAsync(folder);

            _services.State.State.MusicFolderPath = folder;
            _services.State.Save();
        }

        private async Task ScanFolderAsync(string folder)
        {
            var progress = new Progress<string>(message => StatusText = message);

            await _services.Library.ScanFolderAsync(folder, progress);

            _services.Playlists.RelinkTo(_services.Library.Songs);
            ApplySearch();

            StatusText = $"Found {_services.Library.Songs.Count} songs";
        }

        /// <summary>Handles files and folders dropped onto the window.</summary>
        public async Task AddDroppedPathsAsync(IEnumerable<string> paths)
        {
            StatusText = "Adding dropped files...";

            await _services.Library.AddFilesAsync(paths);

            _services.Playlists.RelinkTo(_services.Library.Songs);
            ApplySearch();

            StatusText = $"Library now has {_services.Library.Songs.Count} songs";
        }

        private void ApplySearch()
        {
            var results = _services.Library.Search(SearchText);

            DisplayedSongs.Clear();
            foreach (var song in results)
                DisplayedSongs.Add(song);

            if (!string.IsNullOrWhiteSpace(SearchText))
                StatusText = $"Found {results.Count} songs";
        }

        #endregion

        #region Playlists

        private void CreatePlaylist()
        {
            var details = _dialogs.AskForPlaylistDetails();
            if (details == null || string.IsNullOrWhiteSpace(details.Name))
                return;

            _services.Playlists.Create(details.Name, details.Description, details.Cover);
            StatusText = $"Created playlist '{details.Name}'";
        }

        private void OpenPlaylist(Playlist? playlist)
        {
            if (playlist != null)
                _dialogs.ShowPlaylistDetails(playlist);
        }

        private void DeletePlaylist(Playlist? playlist)
        {
            if (playlist == null)
                return;

            if (!_dialogs.Confirm($"Delete playlist '{playlist.Name}'?", "Confirm Delete"))
                return;

            _services.Playlists.Delete(playlist);
            StatusText = $"Deleted playlist '{playlist.Name}'";
        }

        private void AddToPlaylist(Song? song)
        {
            if (song == null)
                return;

            if (Playlists.Count == 0)
            {
                _dialogs.ShowMessage("Create a playlist first.", "No Playlists");
                return;
            }

            var playlist = _dialogs.PickPlaylist(Playlists);
            if (playlist == null)
                return;

            _services.Playlists.AddSong(playlist, song);
            StatusText = $"Added '{song.Title}' to '{playlist.Name}'";
        }

        #endregion

        #region Songs and transport

        private void EditSong(Song? song)
        {
            if (song == null)
                return;

            var result = _dialogs.EditSong(song);
            if (result == null)
                return;

            song.Title = result.Edited.Title;
            song.Artist = result.Edited.Artist;
            song.Album = result.Edited.Album;
            song.Genre = result.Edited.Genre;
            song.Year = result.Edited.Year;

            if (result.AlbumArtChanged)
                song.AlbumArt = ImageUtils.CreateThumbnail(result.RawAlbumArt);

            try
            {
                _services.Library.UpdateMetadata(song, result.AlbumArtChanged, result.RawAlbumArt);
                StatusText = $"Updated metadata for '{song.Title}'";
            }
            catch (Exception ex)
            {
                _dialogs.ShowMessage(
                    $"Song information updated in the app, but saving to the file failed: {ex.Message}",
                    "Warning", DialogIcon.Warning);

                StatusText = $"Updated '{song.Title}' (app only)";
            }
        }

        private void PlaySong(Song? song)
        {
            if (song == null)
                return;

            Invoke(() => Playback.PlaySongFromCollection(song, DisplayedSongs));
        }

        private void PlayPause()
        {
            if (Playback.PlaybackState == PlaybackState.Playing)
            {
                Playback.Pause();
                return;
            }

            if (Playback.CurrentSong == null && SelectedSong != null)
            {
                PlaySong(SelectedSong);
                return;
            }

            if (Playback.CurrentSong == null && DisplayedSongs.Count > 0)
            {
                PlaySong(DisplayedSongs[0]);
                return;
            }

            Invoke(Playback.Play);
        }

        private void CycleRepeat()
        {
            Playback.RepeatMode = Playback.RepeatMode switch
            {
                RepeatMode.Off => RepeatMode.RepeatAll,
                RepeatMode.RepeatAll => RepeatMode.RepeatOne,
                _ => RepeatMode.Off
            };
        }

        /// <summary>Runs a transport action, surfacing playback failures as a message.</summary>
        private void Invoke(Action action)
        {
            try
            {
                action();
            }
            catch (PlaybackException ex)
            {
                _dialogs.ShowMessage(ex.Message, "Playback Error", DialogIcon.Warning);
                StatusText = ex.Message;
            }
        }

        #endregion

        #region Karaoke

        private void UpdateKaraokeState()
        {
            RunOnUi(() =>
            {
                var song = Playback.CurrentSong;

                KaraokeState = song == null ? KaraokeState.Unavailable
                    : Karaoke.IsInKaraokeMode ? KaraokeState.Active
                    : Karaoke.StemsExist(song) ? KaraokeState.Ready
                    : KaraokeState.NeedsSeparation;
            });
        }

        private async Task ToggleKaraokeAsync()
        {
            if (Playback.CurrentSong == null)
            {
                _dialogs.ShowMessage("Play a song first, then switch to karaoke mode.", "No Song Playing");
                return;
            }

            if (Karaoke.IsInKaraokeMode)
            {
                StatusText = Karaoke.Disable()
                    ? "Switched back to the original track"
                    : "Could not switch back to the original track";
                return;
            }

            // Stems already on disk: no separation, no overlay, instant switch
            if (Karaoke.StemsExist(Playback.CurrentSong))
            {
                StatusText = await Karaoke.EnableAsync()
                    ? "Karaoke mode on - playing the instrumental"
                    : "Could not load the instrumental track";
                return;
            }

            if (!await EnsureDependenciesAsync())
                return;

            await RunJobAsync("Starting audio separation...",
                "Separating vocals and instruments. This can take several minutes.",
                async token =>
            {
                var success = await Karaoke.EnableAsync(token);

                StatusText = success
                    ? "Karaoke mode on - playing the instrumental"
                    : "Karaoke mode was not activated";
            });
        }

        private async Task BatchSeparateAsync()
        {
            var songs = DisplayedSongs.Where(s => !Karaoke.StemsExist(s)).ToList();
            if (songs.Count == 0)
            {
                _dialogs.ShowMessage("Every song in the current view already has stems.", "Nothing To Do");
                return;
            }

            var confirmed = _dialogs.Confirm(
                $"Separate {songs.Count} songs?\n\nThis runs in the background and can take several minutes per song. " +
                "You can cancel at any time.",
                "Batch Separation");

            if (!confirmed || !await EnsureDependenciesAsync())
                return;

            await RunJobAsync("Starting batch separation...",
                "Separating vocals and instruments. This can take several minutes per song.",
                async token =>
            {
                var progress = new Progress<string>(message => LoadingText = message);
                await Karaoke.PreprocessAsync(songs, progress, token);
                StatusText = "Batch separation finished";
            });
        }

        private async Task ExportStemsAsync()
        {
            var song = SelectedSong ?? Playback.CurrentSong;
            if (song == null)
            {
                _dialogs.ShowMessage("Select a song to export.", "No Song Selected");
                return;
            }

            if (!await EnsureDependenciesAsync())
                return;

            var instrumental = _dialogs.Confirm(
                $"Export the instrumental for '{song.Title}'?\n\nChoose No to export the vocals instead.",
                "Export Stems");

            var suffix = instrumental ? "Instrumental" : "Vocals";
            var target = _dialogs.PickSaveFile("Export Stems", "WAV files (*.wav)|*.wav", $"{song.Title} - {suffix}.wav");
            if (string.IsNullOrEmpty(target))
                return;

            await RunJobAsync($"Exporting the {suffix.ToLowerInvariant()}...",
                "Separating vocals and instruments if this song has not been processed yet.",
                async token =>
            {
                var success = await Karaoke.ExportAsync(song, target, instrumental, token);

                _dialogs.ShowMessage(
                    success ? $"{suffix} exported successfully." : $"Could not export the {suffix.ToLowerInvariant()}.",
                    success ? "Export Complete" : "Export Failed",
                    success ? DialogIcon.Information : DialogIcon.Error);
            });
        }

        #endregion

        #region Mood detection

        private async Task AnalyzeMoodAsync()
        {
            var song = SelectedSong ?? Playback.CurrentSong;
            if (song == null)
            {
                _dialogs.ShowMessage("Select a song to analyse.", "No Song Selected");
                return;
            }

            var cached = MoodAnalyzer.GetMood(song.FilePath);
            if (cached != null)
            {
                _dialogs.ShowMessage($"{song.Title}\n\n{cached.Describe()}", "Mood");
                return;
            }

            if (!await EnsureMoodDependenciesAsync())
                return;

            await RunJobAsync($"Analysing '{song.Title}'...",
                "Measuring tempo, key, loudness and brightness to place the track on the mood map.",
                async token =>
            {
                var analysis = await MoodAnalyzer.AnalyzeAsync(song, token);

                if (analysis == null)
                {
                    StatusText = $"Could not analyse '{song.Title}'";
                    _dialogs.ShowMessage(
                        $"'{song.Title}' could not be analysed. The file may be corrupt or in an unsupported format.",
                        "Mood Detection Failed", DialogIcon.Warning);
                    return;
                }

                StatusText = $"{song.Title}: {MoodAnalysis.MoodLabel(analysis.Mood)}";
            });
        }

        private async Task AnalyzeAllMoodsAsync()
        {
            var songs = DisplayedSongs.Where(s => MoodAnalyzer.GetMood(s.FilePath) == null).ToList();
            if (songs.Count == 0)
            {
                _dialogs.ShowMessage("Every song in the current view has already been analysed.", "Nothing To Do");
                return;
            }

            var confirmed = _dialogs.Confirm(
                $"Analyse the mood of {songs.Count} songs?\n\n" +
                "This takes a few seconds per song and can be cancelled at any time.",
                "Mood Detection");

            if (!confirmed || !await EnsureMoodDependenciesAsync())
                return;

            await RunJobAsync("Starting mood detection...",
                "Measuring tempo, key, loudness and brightness for each track.",
                async token =>
            {
                var progress = new Progress<string>(message => LoadingText = message);
                await MoodAnalyzer.AnalyzeManyAsync(songs, progress, token);
                StatusText = "Mood detection finished";
            });
        }

        /// <summary>
        /// Turns the analysed library into one playlist per mood - the "automatic playlist
        /// generation by mood" the feature was meant to provide.
        /// </summary>
        private void CreateMoodPlaylists()
        {
            var groups = _services.Library.Songs
                .Where(s => s.Mood != Mood.Unknown)
                .GroupBy(s => s.Mood)
                .ToList();

            if (groups.Count == 0)
            {
                _dialogs.ShowMessage(
                    "No songs have been analysed yet. Run \"Detect Moods\" first.",
                    "Nothing To Group");
                return;
            }

            var added = 0;

            foreach (var group in groups)
            {
                var name = MoodPlaylistName(group.Key);

                var playlist = Playlists.FirstOrDefault(p => p.Name == name)
                               ?? _services.Playlists.Create(name, "Grouped automatically by detected mood");

                foreach (var song in group.Where(song => !playlist.Songs.Any(s => s.Equals(song))))
                {
                    playlist.Songs.Add(song);
                    added++;
                }
            }

            // One write at the end rather than one per song
            _services.Playlists.Save();

            StatusText = added == 0
                ? "Mood playlists were already up to date"
                : $"Added {added} songs across {groups.Count} mood playlists";
        }

        private static string MoodPlaylistName(Mood mood) => mood switch
        {
            Mood.Happy => "🙂 Happy",
            Mood.Energetic => "⚡ Energetic",
            Mood.Calm => "🌙 Calm",
            Mood.Sad => "💧 Sad",
            _ => "Unknown"
        };

        private async Task<bool> EnsureMoodDependenciesAsync()
        {
            StatusText = "Checking the mood detection setup...";

            var check = await MoodAnalyzer.CheckDependenciesAsync();
            StatusText = check.IsSatisfied ? "Ready" : "Mood detection is not set up";

            if (!check.IsSatisfied)
                _dialogs.ShowMessage(check.Message, "Mood Detection Unavailable", DialogIcon.Warning);

            return check.IsSatisfied;
        }

        #endregion

        #region Shared job plumbing

        /// <summary>
        /// Confirms Python, the separation package and FFmpeg are all present, and explains
        /// precisely what is missing when they are not.
        /// </summary>
        private async Task<bool> EnsureDependenciesAsync()
        {
            StatusText = "Checking the audio separation setup...";

            var check = await Karaoke.CheckDependenciesAsync();
            StatusText = check.IsSatisfied ? "Ready" : "Audio separation is not set up";

            if (!check.IsSatisfied)
                _dialogs.ShowMessage(check.Message, "Audio Separation Unavailable", DialogIcon.Warning);

            return check.IsSatisfied;
        }

        private async Task RunJobAsync(string overlayTitle, string overlayDetails, Func<CancellationToken, Task> work)
        {
            _separationCts = new CancellationTokenSource();
            IsBusy = true;
            ShowOverlay(overlayTitle, overlayDetails);

            try
            {
                await work(_separationCts.Token);
            }
            catch (OperationCanceledException)
            {
                StatusText = "Cancelled";
            }
            catch (Exception ex)
            {
                _dialogs.ShowMessage(ex.Message, "Error", DialogIcon.Error);
                StatusText = "The operation failed";
            }
            finally
            {
                IsBusy = false;
                HideOverlay();

                _separationCts?.Dispose();
                _separationCts = null;

                UpdateKaraokeState();
            }
        }

        private void CancelJob()
        {
            _separationCts?.Cancel();
            StatusText = "Cancelling...";
        }

        private void OnSeparationProgress(object? sender, AudioProgressEventArgs e)
        {
            RunOnUi(() =>
            {
                StatusText = e.Message;

                // The overlay is hidden by RunJobAsync when the whole operation ends, not
                // here: a batch run reports completion once per song and would otherwise close
                // the overlay while the rest of the queue is still being processed.
                if (e.IsError)
                {
                    _dialogs.ShowMessage(e.Message, "Audio Separation Error", DialogIcon.Error);
                    return;
                }

                if (e.IsCompleted)
                    return;

                if (string.IsNullOrEmpty(e.Message))
                    return;

                LoadingText = e.Message;

                if (e.Percentage.HasValue)
                {
                    IsLoadingProgressIndeterminate = false;
                    LoadingProgress = e.Percentage.Value;
                    LoadingDetails = $"{e.Percentage}% complete. Separation runs on the CPU and can take several minutes.";
                }
                else
                {
                    LoadingDetails = "Separating vocals and instruments. This can take several minutes.";
                }
            });
        }

        private void ShowOverlay(string message, string details)
        {
            LoadingText = message;
            LoadingDetails = details;
            LoadingProgress = 0;
            IsLoadingProgressIndeterminate = true;
            IsOverlayVisible = true;
        }

        private void HideOverlay()
        {
            IsOverlayVisible = false;
        }

        #endregion

        /// <summary>
        /// Marshals to the UI thread when there is one. Progress arrives on the process output
        /// reader thread, and that thread must never be blocked - it is what keeps the Python
        /// pipes drained.
        /// </summary>
        private static void RunOnUi(Action action)
        {
            var dispatcher = Application.Current?.Dispatcher;

            if (dispatcher == null || dispatcher.CheckAccess())
                action();
            else
                dispatcher.InvokeAsync(action);
        }
    }
}
