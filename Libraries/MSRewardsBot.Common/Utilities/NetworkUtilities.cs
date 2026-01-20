namespace MSRewardsBot.Common.Utilities
{
    public class NetworkUtilities
    {
        public static string GetConnectionString(bool https, string host, string port, bool addHub = false)
        {
            string protocol = https ? "https" : "http";
            string hub = addHub ? "/cmdhub" : "";

            return $"{protocol}://{host}:{port}{hub}";
        }
    }
}
