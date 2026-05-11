using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MSRewardsBot.Common.DataEntities.Accounting;

namespace MSRewardsBot.Common.DataEntities.Interfaces
{
    public interface IBotAPI
    {
        public Task<bool> LoginWithToken(Guid token);
        public Task<Guid> Login(User user);
        public Task<Guid> Register(User user);
        public Task<bool> Logout(Guid token);

        public Task<User> GetUserInfo(Guid token);

        public Task<bool> InsertMSAccount(Guid token, MSAccount account);
        public Task<bool> DeleteMSAccount(Guid token, int msAccountId);
        public Task<bool> UpdateMSAccountCookies(Guid token, int msAccountId, List<AccountCookie> cookies);
    }
}
