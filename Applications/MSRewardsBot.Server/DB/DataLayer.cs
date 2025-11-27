using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using MSRewardsBot.Common.DataEntities.Accounting;

namespace MSRewardsBot.Server.DB
{
    public partial class DataLayer : IDisposable
    {
        private ILogger<DataLayer> _logger;
        private MSRBContext _db { get; set; }


        public DataLayer(ILogger<DataLayer> logger)
        {
            _logger = logger;

            _db = new MSRBContext();
            _db.Database.EnsureCreated();

            InitUpdater();
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
            if(_timer != null)
            {
                _timer.Elapsed -= PollingFileVersion_Elapsed;
                _timer.Enabled = false;
                _timer.Dispose();
            }

            _db?.Dispose();
        }
    }
}
