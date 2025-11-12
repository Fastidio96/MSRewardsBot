using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using MSRewardsBot.Common.DataEntities.Accounting;

namespace MSRewardsBot.Server.DB
{
    public partial class DataLayer : IDisposable
    {
        private MSRBContext _db { get; set; }


        public DataLayer()
        {
            _db = new MSRBContext();
            _db.Database.EnsureCreated();
        }

        public List<MSAccount> GetAllMSAccounts()
        {
            using (MSRBContext context = new MSRBContext())
            {
                return context.Accounts
                    .AsNoTracking()
                    .Include(m => m.User)
                    .Include(m => m.Cookies)
                    .ToList();
            }
        }

        public bool InsertMSAccount(MSAccount msAccount)
        {
            using (MSRBContext context = new MSRBContext())
            {
                context.Accounts.Add(msAccount);
                return context.SaveChanges() > 0;
            }
        }

        public bool UpdateMSAccount(MSAccount account)
        {
            using (MSRBContext context = new MSRBContext())
            {
                MSAccount acc = context.Accounts.AsNoTracking().FirstOrDefault(a => a.DbId == account.DbId);
                if (acc == null)
                {
                    return false;
                }

                context.Accounts.Update(account);
                return context.SaveChanges() > 0;
            }
        }


        public void Dispose()
        {
            _db.Dispose();
        }
    }
}
