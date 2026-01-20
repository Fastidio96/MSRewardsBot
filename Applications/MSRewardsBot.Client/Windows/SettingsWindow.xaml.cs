using System;
using System.Net;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using MSRewardsBot.Client.DataEntities;

namespace MSRewardsBot.Client.Windows
{
    /// <summary>
    /// Interaction logic for SettingsWindow.xaml
    /// </summary>
    public partial class SettingsWindow : Window
    {
        private readonly ViewModel _viewModel;
        private readonly AppData _appData;

        public SettingsWindow(ViewModel viewModel, AppData data)
        {
            InitializeComponent();

            _viewModel = viewModel;
            _appData = data;

            this.DataContext = _appData;
            this.Loaded += SettingsWindow_Loaded;
        }

        private void SettingsWindow_Loaded(object sender, RoutedEventArgs e)
        {
            this.Loaded -= SettingsWindow_Loaded;
            if(string.IsNullOrEmpty(txtSrvHost.Text) || string.IsNullOrEmpty(txtSrvPort.Text))
            {
                btnSubmit.IsEnabled = false;
            }

            txtSrvHost.Focus();
        }

        private void txtSrvPort_TextChanged(object sender, TextChangedEventArgs e)
        {
            txtSrvHost.Text = txtSrvHost.Text.Trim().ToLower();
            btnSubmit.IsEnabled = _viewModel.IsValidConnectionSettings(txtSrvHost.Text, txtSrvPort.Text);
        }

        private void txtSrvHost_TextChanged(object sender, TextChangedEventArgs e)
        {
            txtSrvHost.Text = txtSrvHost.Text.Trim();
            btnSubmit.IsEnabled = _viewModel.IsValidConnectionSettings(txtSrvHost.Text, txtSrvPort.Text);
        }

        private async void btnSubmit_Click(object sender, RoutedEventArgs e)
        {
            _appData.ServerHost = txtSrvHost.Text;
            _appData.ServerPort = txtSrvPort.Text;
            _appData.IsHttpsEnabled = chkProtocol.IsEnabled;

            if (!await _viewModel.SaveSettings())
            {
                return;
            }

            this.Close();
        }
    }
}
