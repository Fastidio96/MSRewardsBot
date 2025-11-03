using System;
using System.Linq;
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


        public bool InsertMSAccount(MSAccount msAccount)
        {
            _db.Accounts.Add(msAccount);
            return _db.SaveChanges() > 0;
        }

        public bool UpdateMSAccount(MSAccount account)
        {
            MSAccount acc = _db.Accounts.FirstOrDefault(a => a.DbId == account.DbId);
            if (acc == null)
            {
                return false;
            }

            _db.Accounts.Update(account);
            return _db.SaveChanges() > 0;
        }


        public void Dispose()
        {
            _db.Dispose();
        }
    }
}
