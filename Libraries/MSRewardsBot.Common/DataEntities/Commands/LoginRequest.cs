namespace MSRewardsBot.Common.DataEntities.Commands
{
    public class LoginRequest : CommandRequest
    {
        public string Username { get; set; }
        public string Password { get; set; }
    }
}
