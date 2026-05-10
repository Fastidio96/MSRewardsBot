using System;
using System.Diagnostics;
using System.Windows;
using MSRewardsBot.Client.DataEntities;
using MSRewardsBot.Common.DataEntities.Accounting;

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

            if (!AppConstants.IS_PRODUCTION)
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

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            this.Loaded -= MainWindow_Loaded;
            _appInfo.Accounts.CollectionChanged += Accounts_CollectionChanged;

            _splashScreenWindow?.Hide();

            if (cmbAcc.IsEnabled && cmbAcc.SelectedItem == null)
            {
                cmbAcc.SelectedIndex = 0;
            }
        }

        private void BtnAddAcc_Click(object sender, RoutedEventArgs e)
        {
            _vm.AddMSAccount();
        }

        private void BtnRefreshAcc_Click(object sender, RoutedEventArgs e)
        {
            if (_appInfo.SelectedAccount == null)
            {
                return;
            }

            _vm.RefreshMSAccountCookies(_appInfo.SelectedAccount);
        }

        private async void BtnDeleteAcc_Click(object sender, RoutedEventArgs e)
        {
            MSAccount account = _appInfo.SelectedAccount;
            if (account == null)
            {
                return;
            }

            string label = string.IsNullOrWhiteSpace(account.Email) ? "this account" : account.Email;
            MessageBoxResult res = MessageBox.Show(
                $"Delete {label}?\nThis will remove the account and all its cookies from the server. The action cannot be undone.",
                "Delete MS account",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning,
                MessageBoxResult.No);

            if (res != MessageBoxResult.Yes)
            {
                return;
            }

            if (!await _vm.DeleteMSAccount(account))
            {
                Utils.ShowMessage("Unable to delete the account.");
            }
        }

        private async void BtnLogout_Click(object sender, RoutedEventArgs e)
        {
            await _vm.Logout();
        }

        private async void MainWindow_Closing(object? sender, System.ComponentModel.CancelEventArgs e)
        {
            e.Cancel = true;

            _appInfo.Accounts.CollectionChanged -= Accounts_CollectionChanged;
            this.Closing -= MainWindow_Closing;

            await _vm.DisposeAsync();
            Environment.Exit(0);
        }

        private void BtnSettings_Click(object sender, RoutedEventArgs e)
        {
            _vm.EditSettings();
        }

        private void Hyperlink_RequestNavigate(object sender, System.Windows.Navigation.RequestNavigateEventArgs e)
        {
            Process.Start(new ProcessStartInfo(e.Uri.AbsoluteUri)
            {
                UseShellExecute = true
            });

            e.Handled = true;
        }

        private void BtnUpdate_Click(object sender, RoutedEventArgs e)
        {
            if (!_appInfo.UpdateAvailable)
            {
                return;
            }

            _vm.ApplyUpdate();
        }
    }
}