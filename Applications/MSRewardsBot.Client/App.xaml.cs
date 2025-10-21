using System;
using System.Threading.Tasks;
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
        private UserLoginWindow _userLoginWindow;
        private ViewModel _viewModel;

        public App()
        {
            this.DispatcherUnhandledException += App_DispatcherUnhandledException;
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

            _userLoginWindow = new UserLoginWindow(_viewModel, _splashScreenWindow);
            _userLoginWindow.Closed += UserLoginWindow_Closed;
            _userLoginWindow.Show();
        }

        private void UserLoginWindow_Closed(object? sender, System.EventArgs e)
        {
            _userLoginWindow.Closed -= UserLoginWindow_Closed;
            _mainWindow.Show();
        }

        private void App_Exit(object sender, ExitEventArgs e)
        {
            this.Exit -= App_Exit;
        }

        private void App_DispatcherUnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
        {
            e.Handled = true;
            _splashScreenWindow?.Hide();

            MessageBoxResult res = MessageBox.Show(e.Exception.Message, "Unhandled exception occurred", MessageBoxButton.OK, MessageBoxImage.Error);
            if (res == MessageBoxResult.OK)
            {
                Environment.Exit(-1);
            }
        }
    }
}
