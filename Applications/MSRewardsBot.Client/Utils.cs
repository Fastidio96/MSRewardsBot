using System.Windows;

namespace MSRewardsBot.Client
{
    public class Utils
    {
        public static MessageBoxResult ShowMessage(string message, MessageBoxImage image = MessageBoxImage.Error)
        {
            return ShowMessage(message, "Error", image);
        }

        public static MessageBoxResult ShowMessage(string message, string title, MessageBoxImage image = MessageBoxImage.Error)
        {
            return MessageBox.Show(message, title, MessageBoxButton.OK, image);
        }
    }
}
