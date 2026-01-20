using System.Diagnostics;
using System.Windows;
using MSRewardsBot.Client.DataEntities;

namespace MSRewardsBot.Client.Windows
{
    /// <summary>
    /// Interaction logic for SplashScreenWindow.xaml
    /// </summary>
    public partial class SplashScreenWindow : Window
    {
        private readonly ViewModel _viewModel;
        private readonly AppInfo _appInfo;

        public SplashScreenWindow(ViewModel vm, AppInfo appInfo)
        {
            InitializeComponent();

            _viewModel = vm;
            _appInfo = appInfo;
            DataContext = _appInfo;
        }

        private void BtnSettings_Click(object sender, RoutedEventArgs e)
        {
            _viewModel.EditSettings();
        }

        private void Hyperlink_RequestNavigate(object sender, System.Windows.Navigation.RequestNavigateEventArgs e)
        {
            Process.Start(new ProcessStartInfo(e.Uri.AbsoluteUri)
            {
                UseShellExecute = true
            });

            e.Handled = true;
        }
    }
}
