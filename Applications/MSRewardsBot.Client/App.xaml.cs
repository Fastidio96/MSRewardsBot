using System;
using System.Diagnostics;
using System.Windows;

namespace MSRewardsBot.Client
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        private ViewModel _viewModel;

        public App()
        {
            this.DispatcherUnhandledException += App_DispatcherUnhandledException;
            this.Startup += App_Startup;
        }

        private void App_Startup(object sender, StartupEventArgs e)
        {
            this.Startup -= App_Startup;

            _viewModel = new ViewModel();
            _viewModel.Init();
        }

        private void App_DispatcherUnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
        {
            e.Handled = true;
            _viewModel.Dispose();

            MessageBoxResult res = MessageBox.Show(e.Exception.Message, "Unhandled exception occurred", MessageBoxButton.OK, MessageBoxImage.Error);
            if (res == MessageBoxResult.OK)
            {
                this.DispatcherUnhandledException -= App_DispatcherUnhandledException;
                Environment.Exit(-1);
            }
        }
    }
}
