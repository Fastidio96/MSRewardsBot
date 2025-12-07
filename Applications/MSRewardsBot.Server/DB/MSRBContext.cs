using Microsoft.EntityFrameworkCore;
using MSRewardsBot.Common.DataEntities.Accounting;

namespace MSRewardsBot.Server.DB
{
    public class MSRBContext : DbContext
    {
        public DbSet<MSAccount> Accounts { get; set; }
        public DbSet<AccountCookie> AccountCookies { get; set; }
        public DbSet<User> Users { get; set; }
        public DbSet<UserAuthToken> UserAuthTokens { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseSqlite($"Data Source={Utils.GetDBFile()};Cache=Shared");
#if DEBUG
            //optionsBuilder.EnableSensitiveDataLogging();
#endif

            base.OnConfiguring(optionsBuilder);
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<User>();
            modelBuilder.Entity<UserAuthToken>()
                .HasOne(t => t.User)
                .WithOne(u => u.AuthToken)
                .HasForeignKey<UserAuthToken>(t => t.UserId);
            modelBuilder.Entity<MSAccount>()
                .HasOne(m => m.User)
                .WithMany(u => u.MSAccounts)
                .HasForeignKey(m => m.UserId);
            modelBuilder.Entity<MSAccount>()
                .HasMany(m => m.Cookies)
                .WithOne(c => c.MSAccount)
                .HasForeignKey(c => c.MSAccountId);
            modelBuilder.Entity<AccountCookie>();

            base.OnModelCreating(modelBuilder);
        }
    }
}
