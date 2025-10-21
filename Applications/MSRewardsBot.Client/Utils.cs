using System.Windows;

namespace MSRewardsBot.Client
{
    public class Utils
    {
        public static MessageBoxResult ShowError(string message, MessageBoxImage image = MessageBoxImage.Error)
        {
            return MessageBox.Show(message, "Error", MessageBoxButton.OK, image);
        }
    }
}
