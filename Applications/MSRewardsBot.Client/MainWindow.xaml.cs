using System.Windows;
using MSRewardsBot.Client.Services;

namespace MSRewardsBot.Client
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private readonly ViewModel _vm;

        public MainWindow(ViewModel vm)
        {
            InitializeComponent();

            _vm = vm;
        }




    }
}