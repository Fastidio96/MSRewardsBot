using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using MSRewardsBot.Common.DataEntities.Accounting;

namespace MSRewardsBot.Server.DB
{
    public partial class DataLayer
    {
        private readonly MSRBContext _db;


        public DataLayer(MSRBContext db)
        {
            _db = db;
        }

        public List<MSAccount> GetAllMSAccounts()
        {
            return _db.Accounts
                    .AsNoTracking()
                    .Include(m => m.User)
                    .Include(m => m.Cookies)
                    .ToList();
        }

        public bool InsertMSAccount(MSAccount msAccount)
        {
            _db.Accounts.Add(msAccount);
            return _db.SaveChanges() > 0;
        }

        public bool UpdateMSAccount(MSAccount account)
        {
            MSAccount tracked = _db.Accounts.FirstOrDefault(a => a.DbId == account.DbId);
            if (tracked == null)
            {
                return false;
            }

            tracked.Email = account.Email;

            return _db.SaveChanges() >= 0;
        }
    }
}
