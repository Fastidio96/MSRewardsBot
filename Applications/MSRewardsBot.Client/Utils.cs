using System.Diagnostics;
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

        public static bool KillWebViewProcess()
        {
            try
            {
                Process[] ps = Process.GetProcessesByName("msedgewebview2");
                foreach (Process p in ps)
                {
                    p.Kill();
                }

                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}
