namespace MSRewardsBot.Common
{
    public class Env
    {
#if DEBUG
        public const string PUBLIC_SERVER_HOST = "localhost";
        public const string SERVER_BIND_IP = "0.0.0.0";
        public const int SERVER_PORT = 10500;
        public const string SERVER_HUB_NAME = "cmdhub";
        public const bool IS_HTTPS_ENABLED = false;
#else
        public const string PUBLIC_SERVER_HOST = "msbot.laptopick.com";
        public const string SERVER_BIND_IP = "0.0.0.0";
        public const int SERVER_PORT = 10500;
        public const string SERVER_HUB_NAME = "cmdhub";
        public const bool IS_HTTPS_ENABLED = false;
#endif

        public static string GetServerConnection()
        {
            string protocol = IS_HTTPS_ENABLED ? "https" : "http";
#if DEBUG
            return $"{protocol}://{PUBLIC_SERVER_HOST}:{SERVER_PORT}";
#else
            return $"{protocol}://0.0.0.0:{SERVER_PORT}";
#endif
        }

        public static string GetClientConnection()
        {
            string protocol = IS_HTTPS_ENABLED ? "https" : "http";
            return $"{protocol}://{PUBLIC_SERVER_HOST}:{SERVER_PORT}/{SERVER_HUB_NAME}";
        }
    }
}
