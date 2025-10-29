using System;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Logging;
using MSRewardsBot.Common.DataEntities.Accounting;
using MSRewardsBot.Server.DB;

namespace MSRewardsBot.Server.Core
{
    public class BusinessLayer : IDisposable
    {
        private const string PWD_SALT = @"C3tn5yrPYPiAv9Pm59L4Y1tArw6eEjYK";

        private readonly ILogger<BusinessLayer> _logger;
        private readonly DataLayer _data;

        public BusinessLayer(ILogger<BusinessLayer> logger)
        {
            _logger = logger;
            _data = new DataLayer();
        }

        public Guid Login(User input)
        {
            if (input == null || string.IsNullOrEmpty(input.Username) || string.IsNullOrEmpty(input.Password))
            {
                _logger.Log(LogLevel.Warning, "Login failed. Username/password is empty. {Username}|{Password}", input.Username, input.Password);
                return Guid.Empty;
            }

            User dbUser = _data.GetUser(input.Username);
            if (dbUser == null)
            {
                _logger.Log(LogLevel.Warning, "Login failed. User {Username} does not exist.", input.Username);
                return Guid.Empty;
            }

            if (dbUser.Password != GenerateHashFromPassword(input.Password))
            {
                _logger.Log(LogLevel.Warning, "Login failed. The password do not match for user {User}", input.Username);
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
                _logger.Log(LogLevel.Warning, "Register failed. The username/password does not meet the minimum requirements");
                return Guid.Empty;
            }

            if (_data.IsUsernameAlreadyExists(user.Username))
            {
                _logger.Log(LogLevel.Warning, "Register failed. The username {Username} is already taken", user.Username);
                return Guid.Empty;
            }

            user.Password = GenerateHashFromPassword(user.Password);

            if (!_data.CreateUser(user.Username, user.Password))
            {
                _logger.Log(LogLevel.Error, "Register failed. Cannot create the user {Username}", user.Username);
                return Guid.Empty;
            }

            return _data.GetUserAuthToken(user.Username);
        }

        private string GenerateHashFromPassword(string password)
        {
            using (SHA256 sha = SHA256.Create())
            {
                return Convert.ToBase64String(sha.ComputeHash(Encoding.UTF8.GetBytes(PWD_SALT + password)));
            }
        }


        private bool IsUserLogged(Guid token, out User user)
        {
            user = null;

            if (token == Guid.Empty)
            {
                _logger.Log(LogLevel.Warning, "The given token is empty");
                return false;
            }

            user = _data.GetUser(token);
            if (user == null)
            {
                _logger.Log(LogLevel.Warning, "No token found. {Token}", token);
                return false;
            }

            _logger.Log(LogLevel.Debug, "User {User} logged with token {Token}", user.Username, token);
            return true;
        }

        public User GetUserInfo(Guid token)
        {
            if (!IsUserLogged(token, out User user))
            {
                return null;
            }

            return user;
        }
        

        internal User GetUser(string username)
        {
            return _data.GetUser(username);
        }

        public bool InsertMSAccount(Guid token, MSAccount account)
        {
            if (!IsUserLogged(token, out User user))
            {
                return false;
            }

            account.UserId = user.DbId;
            account.User = user;

            return _data.InsertMSAccount(account);
        }

        public void Dispose()
        {
            _data?.Dispose();
        }
    }
}
