using System.Threading.Tasks;
using System.Windows;
using MSRewardsBot.Client.DataEntities;

namespace MSRewardsBot.Client.Windows
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
            if (cmbAcc.IsEnabled && cmbAcc.SelectedItem == null)
            {
                cmbAcc.SelectedIndex = 0;
            }
        }

        private async void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            this.Loaded -= MainWindow_Loaded;
            _splashScreenWindow.Hide();

            await _vm.GetUserInfo();

            if (cmbAcc.IsEnabled && cmbAcc.SelectedItem == null)
            {
                cmbAcc.SelectedIndex = 0;
            }
        }

        private void BtnAddAcc_Click(object sender, RoutedEventArgs e)
        {
            _vm.AddMSAccount();
        }

        private async void btnLogout_Click(object sender, RoutedEventArgs e)
        {
            await _vm.Logout();
        }
    }
}