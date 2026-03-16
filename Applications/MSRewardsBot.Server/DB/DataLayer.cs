using System;
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
            MSAccount acc = _db.Accounts.AsNoTracking().FirstOrDefault(a => a.DbId == account.DbId);
            if (acc == null)
            {
                return false;
            }

            _db.Accounts.Update(account);
            return _db.SaveChanges() > 0;
        }
    }
}
