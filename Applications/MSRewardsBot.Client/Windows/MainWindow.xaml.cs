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

        public MainWindow(ViewModel vm, SplashScreenWindow splash, AppInfo info)
        {
            InitializeComponent();

            _vm = vm;
            _splashScreenWindow = splash;
            _appInfo = info;

            this.DataContext = _appInfo;
            this.Loaded += MainWindow_Loaded;

            _appInfo.Accounts.CollectionChanged += Accounts_CollectionChanged;
        }

        private void Accounts_CollectionChanged(object? sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            cmbAcc.IsEnabled = _appInfo.Accounts.Count > 0;
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            this.Loaded -= MainWindow_Loaded;
            _splashScreenWindow.Hide();
        }

        private void BtnAddAcc_Click(object sender, RoutedEventArgs e)
        {
            _vm.AddMSAccount();
        }
    }
}