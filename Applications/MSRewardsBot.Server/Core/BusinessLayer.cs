using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Logging;
using MSRewardsBot.Common.DataEntities.Accounting;
using MSRewardsBot.Server.DataEntities;
using MSRewardsBot.Server.DB;

namespace MSRewardsBot.Server.Core
{
    public partial class BusinessLayer
    {
        private readonly ILogger<BusinessLayer> _logger;
        private readonly RealTimeData _rt;
        private readonly DataLayer _data;

        public BusinessLayer(ILogger<BusinessLayer> logger, DataLayer dl, RealTimeData rt)
        {
            _logger = logger;
            _data = dl;
            _rt = rt;
        }

        public bool Login(Guid token)
        {
            return IsUserLogged(token, out _);
        }

        public Guid Login(User input)
        {
            if (input == null || string.IsNullOrEmpty(input.Username) || string.IsNullOrEmpty(input.Password))
            {
                _logger.Log(LogLevel.Warning, "LoginWithToken failed. Username/password is empty");
                return Guid.Empty;
            }

            User dbUser = _data.GetUser(input.Username);
            if (dbUser == null)
            {
                _logger.Log(LogLevel.Warning, "LoginWithToken failed. User {Username} does not exist.", input.Username);
                return Guid.Empty;
            }

            if(!AuthUtils.VerifyPassword(input.Password, dbUser.Password))
            {
                _logger.Log(LogLevel.Warning, "LoginWithToken failed. The password do not match for user {User}", input.Username);
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

            user.Password = AuthUtils.HashPassword(user.Password);

            if (!_data.CreateUser(user.Username, user.Password))
            {
                _logger.Log(LogLevel.Error, "Register failed. Cannot create the user {Username}", user.Username);
                return Guid.Empty;
            }

            return _data.GetUserAuthToken(user.Username);
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

            if (user == null)
            {
                return null;
            }

            foreach (MSAccount acc in user.MSAccounts)
            {
                if (!_rt.CacheMSAccStats.TryGetValue(acc.DbId, out MSAccountServerData data))
                {
                    _logger.LogTrace("Cannot retrieve from server the account instance msaccount id {id}", acc.DbId);
                    continue;
                }

                acc.Stats = data.Stats;
            }

            return user;
        }

        internal List<MSAccount> GetAllMSAccounts()
        {
            return _data.GetAllMSAccounts();
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

            return _data.InsertMSAccount(account);
        }

        internal bool UpdateMSAccount(MSAccount account)
        {
            return _data.UpdateMSAccount(account);
        }

        public bool Logout(Guid token)
        {
            return _data.InvalidateUserAuthToken(token);
        }
    }
}
