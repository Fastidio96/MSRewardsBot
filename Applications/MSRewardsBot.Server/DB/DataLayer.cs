using System;

namespace MSRewardsBot.Server.DB
{
    public class DataLayer : IDisposable
    {
        private MSRBContext _db { get; set; }

        public DataLayer()
        {
            _db = new MSRBContext();
            _db.Database.EnsureCreated();
        }






        public void Dispose()
        {
            _db.Dispose();
        }
    }
}
