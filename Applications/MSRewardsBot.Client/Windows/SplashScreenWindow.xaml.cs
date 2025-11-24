using System.Windows;
using MSRewardsBot.Client.DataEntities;

namespace MSRewardsBot.Client.Windows
{
    /// <summary>
    /// Interaction logic for SplashScreenWindow.xaml
    /// </summary>
    public partial class SplashScreenWindow : Window
    {
        private AppInfo _appInfo;

        public SplashScreenWindow(AppInfo appInfo)
        {
            InitializeComponent();

            _appInfo = appInfo;
            DataContext = _appInfo;
        }
    }
}
