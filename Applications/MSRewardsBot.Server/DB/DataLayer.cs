using System;
using System.Linq;
using MSRewardsBot.Common.DataEntities.Accounting;

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

        #region Accounting

        public User GetUser(string username)
        {
            return _db.Users.FirstOrDefault(u => u.Username == username);
        }

        public User GetUser(Guid authToken)
        {
            return _db.Users.FirstOrDefault(u => u.AuthToken.Token == authToken);
        }

        public Guid GetUserAuthToken(string username)
        {
            User user = _db.Users.FirstOrDefault(u => u.Username == username);
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

        public void CreateUser(string username, string password)
        {
            _db.Users.Add(new User()
            {
                Username = username,
                Password = password,
                
            });

            _db.SaveChanges();
        }

        public bool IsUsernameAlreadyExists(string username)
        {
            return _db.Users.Count(u => u.Username == username) > 0;
        }

        public void UpdateUser(User user)
        {
            _db.Users.Update(user);
            _db.SaveChanges();
        }

        #endregion

        public void Dispose()
        {
            _db.Dispose();
        }
    }
}
