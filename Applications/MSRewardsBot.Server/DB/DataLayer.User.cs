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
            using (MSRBContext context = new MSRBContext())
            {
                return context.Users
                    .AsNoTracking()
                    .Include(u => u.AuthToken)
                    .Include(m => m.MSAccounts)
                        .ThenInclude(c => c.Cookies)
                    .FirstOrDefault(u => u.Username == username);
            }
        }

        public User GetUser(Guid authToken)
        {
            using (MSRBContext context = new MSRBContext())
            {
                return context.Users
                    .AsNoTracking()
                    .Include(u => u.AuthToken)
                    .Include(m => m.MSAccounts)
                        .ThenInclude(c => c.Cookies)
                    .FirstOrDefault(u => u.AuthToken.Token == authToken);
            }
        }

        public bool InvalidateUserAuthToken(Guid token)
        {
            using (MSRBContext context = new MSRBContext())
            {
                UserAuthToken auth = context.UserAuthTokens
                    .AsNoTracking()
                    .FirstOrDefault(t => t.Token == token);
                if (auth == null)
                {
                    return true;
                }

                auth.Token = Guid.Empty;
                context.UserAuthTokens.Update(auth);

                return context.SaveChanges() > 0;
            }
        }

        public Guid GetUserAuthToken(string username)
        {
            using (MSRBContext context = new MSRBContext())
            {
                User user = context.Users
                    .AsNoTracking()
                    .Include(u => u.AuthToken)
                    .FirstOrDefault(u => u.Username == username);
                if (user == null)
                {
                    return Guid.Empty;
                }

                return CreateUserAuthToken(user).Token;
            }
        }

        private UserAuthToken CreateUserAuthToken(User user)
        {
            using (MSRBContext context = new MSRBContext())
            {
                UserAuthToken token = context.UserAuthTokens
                    .AsNoTracking()
                    .FirstOrDefault(t => t.User.Username == user.Username);
                if (token == null)
                {
                    token = context.UserAuthTokens.Add(new UserAuthToken()
                    {
                        CreatedAt = DateTime.Now,
                        LastTimeUsed = DateTime.Now,
                        Token = Guid.NewGuid(),
                        User = user
                    }).Entity;

                    context.SaveChanges();
                }
                else
                {
                    token.LastTimeUsed = DateTime.Now;

                    context.UserAuthTokens.Update(token);
                    context.SaveChanges();
                }

                return token;
            }
        }

        public bool CreateUser(string username, string password)
        {
            using (MSRBContext context = new MSRBContext())
            {
                context.Users.Add(new User()
                {
                    Username = username,
                    Password = password
                });

                return context.SaveChanges() > 0;
            }
        }

        public bool IsUsernameAlreadyExists(string username)
        {
            using (MSRBContext context = new MSRBContext())
            {
                return context.Users
                    .AsNoTracking()
                    .Count(u => u.Username == username) > 0;
            }
        }

        public bool UpdateUser(User user)
        {
            using (MSRBContext context = new MSRBContext())
            {
                context.Users.Update(user);
                return context.SaveChanges() > 0;
            }
        }
    }
}
