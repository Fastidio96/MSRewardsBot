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
                    .AsNoTracking()
                    .Include(u => u.AuthToken)
                    .Include(m => m.MSAccounts)
                        .ThenInclude(c => c.Cookies)
                    .FirstOrDefault(u => u.Username == username);
        }

        public User GetUser(Guid authToken)
        {
            return _db.Users
                    .AsNoTracking()
                    .Include(u => u.AuthToken)
                    .Include(m => m.MSAccounts)
                        .ThenInclude(c => c.Cookies)
                    .FirstOrDefault(u => u.AuthToken.Token == authToken);
        }

        public bool InvalidateUserAuthToken(Guid token)
        {
            UserAuthToken auth = _db.UserAuthTokens
                    .AsNoTracking()
                    .FirstOrDefault(t => t.Token == token);
            if (auth == null)
            {
                return true;
            }

            _db.UserAuthTokens.Remove(auth);

            return _db.SaveChanges() > 0;
        }

        public Guid GetUserAuthToken(string username)
        {
            User user = _db.Users
                    .AsNoTracking()
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
            UserAuthToken token = _db.UserAuthTokens
                    .AsNoTracking()
                    .FirstOrDefault(t => t.User.Username == user.Username);
            if (token == null)
            {
                token = new UserAuthToken()
                {
                    CreatedAt = DateTime.Now,
                    LastTimeUsed = DateTime.Now,
                    Token = Guid.NewGuid(),
                    UserId = user.DbId
                };

                _db.UserAuthTokens.Add(token);

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
                Password = password
            });

            return _db.SaveChanges() > 0;
        }

        public bool IsUsernameAlreadyExists(string username)
        {
            return _db.Users
                    .AsNoTracking()
                    .Count(u => u.Username == username) > 0;
        }

        public bool UpdateUser(User user)
        {
            _db.Users.Update(user);
            return _db.SaveChanges() > 0;
        }
    }
}
