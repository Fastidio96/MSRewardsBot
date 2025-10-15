using System.Windows;
using MSRewardsBot.Client.DataEntities;
using MSRewardsBot.Client.Windows;

namespace MSRewardsBot.Client
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private readonly SplashScreenWindow _splashScreenWindow;
        private readonly ViewModel _vm;
        private AppInfo _appInfo;

        public MainWindow(ViewModel vm, SplashScreenWindow splash)
        {
            InitializeComponent();

            _vm = vm;
            _splashScreenWindow = splash;

            this.Loaded += MainWindow_Loaded;
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            this.Loaded -= MainWindow_Loaded;
            _splashScreenWindow.Close();

            _appInfo = new AppInfo();
            _vm.SetInstanceAppInfo(_appInfo);
        }
    }
}