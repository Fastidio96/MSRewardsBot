using System.Windows;
using MSRewardsBot.Client.Windows;

namespace MSRewardsBot.Client
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        private SplashScreenWindow _splashScreenWindow;
        private MainWindow _mainWindow;
        private ViewModel _viewModel;

        public App()
        {
            this.Startup += App_Startup;
            this.Exit += App_Exit;

            _splashScreenWindow = new SplashScreenWindow();
            _splashScreenWindow.Show();
        }

        private void App_Startup(object sender, StartupEventArgs e)
        {
            this.Startup -= App_Startup;

            _viewModel = new ViewModel();
            _viewModel.Init();

            _mainWindow = new MainWindow(_viewModel, _splashScreenWindow);

            App.Current.MainWindow = _mainWindow;
            _mainWindow.Show();
        }

        private void App_Exit(object sender, ExitEventArgs e)
        {
            this.Exit -= App_Exit;
        }
    }
}
