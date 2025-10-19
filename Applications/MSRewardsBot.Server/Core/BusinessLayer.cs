using System;
using System.Security.Cryptography;
using System.Text;
using MSRewardsBot.Common.DataEntities.Accounting;
using MSRewardsBot.Server.DB;

namespace MSRewardsBot.Server.Core
{
    public class BusinessLayer : IDisposable
    {
        private const string PWD_SALT = @"C3tn5yrPYPiAv9Pm59L4Y1tArw6eEjYK";

        private readonly DataLayer _data;

        public BusinessLayer()
        {
            _data = new DataLayer();
        }

        public Guid Login(User input)
        {
            if (input == null || string.IsNullOrEmpty(input.Username) || string.IsNullOrEmpty(input.Password))
            {
                return Guid.Empty;
            }

            User dbUser = _data.GetUser(input.Username);
            if (dbUser == null)
            {
                return Guid.Empty;
            }

            if (dbUser.Password != GenerateHashFromPassword(input.Password))
            {
                return Guid.Empty;
            }

            return _data.GetUserAuthToken(dbUser.Username);
        }

        public Guid Register(User user)
        {
            if (user == null ||
                user.Username.Length == 0 || user.Username.Length > 32 ||
                user.Password.Length == 0 || user.Password.Length > 32)
            {
                return Guid.Empty;
            }

            if (_data.IsUsernameAlreadyExists(user.Username))
            {
                return Guid.Empty;
            }

            user.Password = GenerateHashFromPassword(user.Password);

            _data.CreateUser(user.Username, user.Password);

            return _data.GetUserAuthToken(user.Username);
        }

        private string GenerateHashFromPassword(string password)
        {
            using (SHA256 sha = SHA256.Create())
            {
                return Convert.ToBase64String(sha.ComputeHash(Encoding.UTF8.GetBytes(PWD_SALT + password)));
            }
        }


        public void Dispose()
        {
            _data?.Dispose();
        }
    }
}
