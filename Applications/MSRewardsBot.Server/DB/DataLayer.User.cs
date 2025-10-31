using System;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using MSRewardsBot.Common.DataEntities.Accounting;

namespace MSRewardsBot.Server.DB
{
    public partial class DataLayer
    {
        public User GetUser(string username)
        {
            return _db.Users
                .Include(u => u.AuthToken)
                .FirstOrDefault(u => u.Username == username);
        }

        public User GetUser(Guid authToken)
        {
            return _db.Users
                .Include(u => u.AuthToken)
                .FirstOrDefault(u => u.AuthToken.Token == authToken);
        }

        public bool InvalidateUserAuthToken(Guid token)
        {
            UserAuthToken auth = _db.UserAuthTokens
                 .FirstOrDefault(t => t.Token == token);
            if (auth == null)
            {
                return true;
            }

            auth.Token = Guid.Empty;
            _db.UserAuthTokens.Update(auth);

            return _db.SaveChanges() > 0;
        }

        public Guid GetUserAuthToken(string username)
        {
            User user = _db.Users
                .Include(u => u.AuthToken)
                .FirstOrDefault(u => u.Username == username);
            if (user == null)
            {
                return Guid.Empty;
            }

            return CreateUserAuthToken(user).Token;
        }

        private UserAuthToken CreateUserAuthToken(User user)
        {
            UserAuthToken token = _db.UserAuthTokens.FirstOrDefault(t => t.User.Username == user.Username);
            if (token == null)
            {
                token = _db.UserAuthTokens.Add(new UserAuthToken()
                {
                    CreatedAt = DateTime.Now,
                    LastTimeUsed = DateTime.Now,
                    Token = Guid.NewGuid(),
                    User = user
                }).Entity;

                _db.SaveChanges();
            }
            else
            {
                token.LastTimeUsed = DateTime.Now;

                _db.UserAuthTokens.Update(token);
                _db.SaveChanges();
            }

            return token;
        }

        public bool CreateUser(string username, string password)
        {
            _db.Users.Add(new User()
            {
                Username = username,
                Password = password,

            });

            return _db.SaveChanges() > 0;
        }

        public bool IsUsernameAlreadyExists(string username)
        {
            return _db.Users.Count(u => u.Username == username) > 0;
        }

        public bool UpdateUser(User user)
        {
            _db.Users.Update(user);
            return _db.SaveChanges() > 0;
        }
    }
}
