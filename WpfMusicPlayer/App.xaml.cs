using System.Windows;
using WpfMusicPlayer.Services;
using WpfMusicPlayer.ViewModels;

namespace WpfMusicPlayer
{
    /// <summary>
    /// Application entry point and composition root: builds the object graph once and hands the
    /// pieces to the main window.
    /// </summary>
    public partial class App : Application
    {
        private AppServices? _services;

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            _services = new AppServices();

            var dialogs = new DialogService(_services);
            var viewModel = new MainViewModel(_services, dialogs);
            var window = new MainWindow(viewModel);

            MainWindow = window;
            window.Show();
        }

        protected override void OnExit(ExitEventArgs e)
        {
            _services?.Dispose();
            base.OnExit(e);
        }
    }
}
