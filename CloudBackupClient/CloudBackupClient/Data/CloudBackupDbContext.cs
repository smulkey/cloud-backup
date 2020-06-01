using CloudBackupClient.Models;
using Microsoft.EntityFrameworkCore;

namespace CloudBackupClient.Data
{
    class CloudBackupDbContext : DbContext
    {
        public CloudBackupDbContext(DbContextOptions<CloudBackupDbContext> options) : base(options)
        {

        }
      
        public DbSet<BackupRun> BackupRuns { get; set; }               
    }
}
