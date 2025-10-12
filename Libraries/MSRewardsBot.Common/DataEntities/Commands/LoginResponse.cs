namespace MSRewardsBot.Common.DataEntities.Commands
{
    public class LoginResponse : CommandResponse
    {
        public bool IsAuthenticated { get; set; }
        public string Token { get; set; }
    }
}
