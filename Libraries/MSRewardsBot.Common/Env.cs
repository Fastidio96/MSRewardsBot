namespace MSRewardsBot.Common
{
    public class Env
    {
#if DEBUG
        public const bool IS_PRODUCTION = false;
        public const string SERVER_HOST = "localhost";
        public const int SERVER_PORT = 10500;
        public const string SERVER_HUB_NAME = "cmdhub";
        public const bool IS_HTTPS_ENABLED = false;
#else
        public const bool IS_PRODUCTION = true;
        public const string SERVER_HOST = "msbot.laptopick.com";
        public const int SERVER_PORT = 10500;
        public const string SERVER_HUB_NAME = "cmdhub";
        public const bool IS_HTTPS_ENABLED = true;
#endif

        public static string GetServerConnection()
        {
#if DEBUG
            return $"http://{SERVER_HOST}:{SERVER_PORT}";
#else
            return $"http://0.0.0.0:{SERVER_PORT}";
#endif
        }

        public static string GetClientConnection()
        {
            string protocol = IS_HTTPS_ENABLED ? "https" : "http";
            return $"{protocol}://{SERVER_HOST}:{SERVER_PORT}/{SERVER_HUB_NAME}";
        }
    }
}
