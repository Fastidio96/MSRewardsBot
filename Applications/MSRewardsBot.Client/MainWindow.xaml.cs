using System.Windows;
using MSRewardsBot.Client.Services;

namespace MSRewardsBot.Client
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private readonly SignalRService _connection;

        public MainWindow()
        {
            InitializeComponent();

            App.Current.MainWindow = this;

            _connection = new SignalRService();
            this.Loaded += MainWindow_Loaded;
        }

        private async void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            await _connection.ConnectAsync();
        }



    }
}