using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using MSRewardsBot.Server.DataEntities;

namespace MSRewardsBot.Server.Automation
{
    public partial class BrowserManager
    {
        public async Task<bool> TestCommand(MSAccountServerData data)
        {
            _logger.LogInformation("Test command started for {User} | {Data}", data.Account.User.Username, data.Account.Email);

            if (!await NavigateToURL(data, BrowserConstants.URL_DASHBOARD))
            {
                return false;
            }

            await WaitRandomMs(1500, 2500);

            await HumanScroll(data.Page);

            if (!await NavigateToURL(data, BrowserConstants.URL_SEARCHES_HOMEPAGE))
            {
                return false;
            }

            await data.Page.Locator(BrowserConstants.SEARCHBAR_TEXTAREA).ClickAsync();

            if (!await WriteSearchAsHuman(data.Page, "0123456789 qwerty asdfgh jkl!"))
            {
                return false;
            }


            return true;
        }
    }
}
