using System;
using System.Collections.Generic;
using System.Text;
using CloudBackupClient.Models;
using Microsoft.EntityFrameworkCore;

namespace CloudBackupClient.Data
{
    class CloudBackupDbContext : DbContext
    {
        //private readonly string connectionString;
                
        //public CloudBackupDbContext(string connString)                 
        //{
        //    this.connectionString = connString;
        //}

        public CloudBackupDbContext(DbContextOptions<CloudBackupDbContext> options) : base(options)
        {

        }

        public DbSet<BackupRun> BackupRuns { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {            
            //if(String.IsNullOrWhiteSpace(this.connectionString))
            //{
            //    throw new Exception("Connection string is null or empty");
            //}
            
            //optionsBuilder.UseSqlite(this.connectionString);
        }
    }
}
