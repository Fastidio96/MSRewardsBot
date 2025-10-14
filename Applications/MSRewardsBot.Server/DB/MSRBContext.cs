using System;
using System.IO;
using Microsoft.EntityFrameworkCore;

namespace MSRewardsBot.Server.DB
{
    public class MSRBContext : DbContext
    {
        //public DbSet<Video> Videos { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseSqlite("FileName=videos.db", (option) =>
            {
                option.MigrationsAssembly(GetFolderDB());
            });

            base.OnConfiguring(optionsBuilder);
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            //modelBuilder.Entity<Video>();

            base.OnModelCreating(modelBuilder);
        }


        private static string GetFolderDB()
        {
            string path = new Uri(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + @"\VideoCollector").LocalPath;
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }

            return path;
        }
    }
}
