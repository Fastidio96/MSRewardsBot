using System;

namespace MSRewardsBot.Server.Core
{
    public partial class BusinessLayer
    {
        public Version LatestClientVersion => _data.LatestClientVersion;

        public bool ClientNeedsToUpdate(Version clientVersion)
        {
            if (_data.LatestClientVersion == null || clientVersion == null)
            {
                return false;
            }

            return _data.LatestClientVersion != clientVersion;
        }

        public byte[] GetClientUpdateFile()
        {
            return _data.GetClientUpdateFile();
        }
    }
}
