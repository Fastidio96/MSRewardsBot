using System;
using System.IO;
using Microsoft.EntityFrameworkCore;
using MSRewardsBot.Common.DataEntities.Accounting;

namespace MSRewardsBot.Server.DB
{
    public class MSRBContext : DbContext
    {
        public DbSet<Account> Accounts { get; set; }
        public DbSet<AccountCookie> AccountCookies { get; set; }
        public DbSet<AccountCookie> Users { get; set; }
        public DbSet<AccountCookie> UserAuthTokens { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseSqlite("FileName=data.db", (option) =>
            {
                option.MigrationsAssembly(GetFolderDB());
            });

            base.OnConfiguring(optionsBuilder);
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Account>();
            modelBuilder.Entity<AccountCookie>();
            modelBuilder.Entity<AccountCookie>();
            modelBuilder.Entity<AccountCookie>();

            base.OnModelCreating(modelBuilder);
        }


        private static string GetFolderDB()
        {
            string path = new Uri(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + @"\msrb").LocalPath;
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }

            return path;
        }
    }
}
