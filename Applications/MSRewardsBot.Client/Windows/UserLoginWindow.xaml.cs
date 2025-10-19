using System;
using System.Threading.Tasks;
using System.Windows;
using MSRewardsBot.Common.DataEntities.Accounting;

namespace MSRewardsBot.Client.Windows
{
    /// <summary>
    /// Interaction logic for UserLoginWindow.xaml
    /// </summary>
    public partial class UserLoginWindow : Window
    {
        private readonly ViewModel _viewModel;
        private readonly SplashScreenWindow _splashScreenWindow;

        public UserLoginWindow(ViewModel viewModel, SplashScreenWindow splashScreenWindow)
        {
            InitializeComponent();

            _viewModel = viewModel;
            _splashScreenWindow = splashScreenWindow;

            this.Loaded += UserLoginWindow_Loaded;
        }

        private void UserLoginWindow_Loaded(object sender, RoutedEventArgs e)
        {
            _splashScreenWindow.Hide();
        }

        private async void BtnSubmit_Click(object sender, RoutedEventArgs e)
        {
            User user = new User()
            {
                Username = txtUsername.Text,
                Password = txtPassword.Text
            };

            Guid token;
            if (btnToggle.IsChecked == true)
            {
                token = await _viewModel.Register(user);
            }
            else
            {
                token = await _viewModel.Login(user);
            }

            if (token == Guid.Empty)
            {
                return;
            }

            _splashScreenWindow.Show();
            this.Close();
        }

        private void ToggleButton_Click(object sender, RoutedEventArgs e)
        {
            if (btnToggle.IsChecked == true)
            {
                SwitchToRegister();
            }
            else
            {
                SwitchToLogin();
            }
        }

        private void SwitchToRegister()
        {
            lblTitle.Text = "Register new account";
            btnToggle.Content = "Log in";
        }

        private void SwitchToLogin()
        {
            lblTitle.Text = "Log in";
            btnToggle.Content = "Register new account";
        }

        private void TxtUsername_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            string test = txtUsername.Text;
            if (test.Length == 0 || test.Length > 32)
            {
                btnSubmit.IsEnabled = false;
                return;
            }

            btnSubmit.IsEnabled = true;
        }

        private void TxtPassword_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            string test = txtPassword.Text;
            if (test.Length == 0 || test.Length > 32)
            {
                btnSubmit.IsEnabled = false;
                return;
            }

            btnSubmit.IsEnabled = true;
        }
    }
}
