using System.Windows;
using MSRewardsBot.Client.DataEntities;
using MSRewardsBot.Common;

namespace MSRewardsBot.Client.Windows
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private readonly SplashScreenWindow _splashScreenWindow;
        private readonly ViewModel _vm;

        private readonly AppInfo _appInfo;

        public MainWindow(ViewModel vm, SplashScreenWindow splash, AppInfo info)
        {
            InitializeComponent();

            _vm = vm;
            _splashScreenWindow = splash;
            _appInfo = info;

            this.DataContext = _appInfo;
            this.Loaded += MainWindow_Loaded;
            this.Closing += MainWindow_Closing;

            if (!Env.IS_PRODUCTION)
            {
#pragma warning disable CS0162 // Unreachable code detected
                Title += " - DEBUG";
#pragma warning restore CS0162 // Unreachable code detected
            }
        }

        private void Accounts_CollectionChanged(object? sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            Dispatcher.Invoke(() =>
            {
                if (cmbAcc.IsEnabled && cmbAcc.SelectedItem == null)
                {
                    cmbAcc.SelectedIndex = 0;
                }
            });
        }

        private async void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            this.Loaded -= MainWindow_Loaded;
            _appInfo.Accounts.CollectionChanged += Accounts_CollectionChanged;

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

        private async void BtnLogout_Click(object sender, RoutedEventArgs e)
        {
            await _vm.Logout();
        }

        private void MainWindow_Closing(object? sender, System.ComponentModel.CancelEventArgs e)
        {
            e.Cancel = true;

            _appInfo.Accounts.CollectionChanged -= Accounts_CollectionChanged;
            this.Closing -= MainWindow_Closing;

            _vm.Dispose();
        }
    }
}