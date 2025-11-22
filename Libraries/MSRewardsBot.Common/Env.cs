namespace MSRewardsBot.Common
{
    public class Env
    {
#if !DEBUG
        public const string SERVER_HOST = "localhost";
        public const int SERVER_PORT = 10500;
        public const string SERVER_HUB_NAME = "cmdhub";
        public const bool IS_HTTPS_ENABLED = false;
#else
        public const string SERVER_HOST = "msbot.laptopick.com";
        public const int SERVER_PORT = 10500;
        public const string SERVER_HUB_NAME = "cmdhub";
        public const bool IS_HTTPS_ENABLED = false;
#endif

        public static string GetConnectionString()
        {
            string protocol = IS_HTTPS_ENABLED ? "https" : "http";
            return $"{protocol}://{SERVER_HOST}:{SERVER_PORT}";
        }

        public static string GetConnectionStringForClient()
        {
            return $"{GetConnectionString()}/{SERVER_HUB_NAME}";
        }
    }
}
