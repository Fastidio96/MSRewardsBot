using System;
using System.IO;
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
            string dbPath = Path.Combine(GetFolderDB(), "data.db");
            optionsBuilder.UseSqlite($"Data Source={dbPath}");

            base.OnConfiguring(optionsBuilder);
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<User>();
                //.Property<int?>("AuthTokenId")
                //.IsRequired(false);
            modelBuilder.Entity<UserAuthToken>()
                .HasOne(t => t.User)
                .WithOne(u => u.AuthToken)
                .HasForeignKey<UserAuthToken>(t => t.UserId);
            modelBuilder.Entity<MSAccount>();
            modelBuilder.Entity<AccountCookie>();

            base.OnModelCreating(modelBuilder);
        }


        private static string GetFolderDB()
        {
            string path = new Uri(AppDomain.CurrentDomain.BaseDirectory + "MSRB").LocalPath;
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }

            return path;
        }
    }
}
