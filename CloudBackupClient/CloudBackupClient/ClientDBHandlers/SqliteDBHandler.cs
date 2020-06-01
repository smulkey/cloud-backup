using CloudBackupClient.Data;
using CloudBackupClient.Models;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;

namespace CloudBackupClient.ClientDBHandlers
{
    public class SqliteDBHandler : IClientDBHandler
    {        
        private DbConnection dbConnection;
        private CloudBackupDbContext dbContext;
        private IServiceProvider serviceProvider;
                
        public  SqliteDBHandler()
        {

        }

        public void Initialize (IServiceProvider serviceProvider)
        {  
            this.serviceProvider = serviceProvider;           
        
            //TODO Add validation
            var connString = serviceProvider.GetService<IConfigurationRoot>().GetConnectionString("SqliteConnString");

            this.Logger.LogInformation("Initializing DB handler");
            
            dbConnection = new SqliteConnection(connString);
            dbConnection.Open();

            var options = new DbContextOptionsBuilder<CloudBackupDbContext>()
                .UseSqlite(dbConnection)
                .Options;
                        
            dbContext = new CloudBackupDbContext(options);
            dbContext.Database.EnsureCreated();         
        }

        public IList<BackupRun> GetOpenBackupRuns()
        {
            var openBackupRuns = this.dbContext.BackupRuns.Where(b => b.BackupRunCompleted == false).ToList<BackupRun>();

            this.Logger.LogInformation($"Returning BackupRun set with {openBackupRuns.Count} entries");

            //SQLite doesn't really support foreign keys so we have to manually re-hydrate
            foreach (var backupRun in openBackupRuns)
            {
                backupRun.BackupFileRefs = dbContext.Set<BackupRunFileRef>().Where(r => r.BackupRunID == backupRun.BackupRunID).ToList<BackupRunFileRef>();
                backupRun.BackupDirectories = dbContext.Set<BackupDirectoryRef>().Where(d => d.BackupRunID == backupRun.BackupRunID).ToList<BackupDirectoryRef>();
            }

            return openBackupRuns;
        }

        public void AddBackupRun(BackupRun backupRun)
        {
            this.Logger.LogInformation("Adding BackupRun with {0} file refs", backupRun.BackupFileRefs == null ? 0 : backupRun.BackupFileRefs.Count);

            this.dbContext.Add<BackupRun>(backupRun);
            dbContext.SaveChanges();
        }

        public void UpdateBackupRun(BackupRun backupRun)
        {            
            this.Logger.LogInformation($"Updating BackupRun set with BackupRunID: {backupRun.BackupRunID}");

            this.dbContext.Update<BackupRun>(backupRun);
            dbContext.SaveChanges();
        }
              
        public void UpdateBackupFileRef(BackupRunFileRef backupRunFileRef)
        {
            this.Logger.LogInformation($"Updating BackupRunFileRef set with BackupRunFileRefID: {backupRunFileRef.BackupRunFileRefID}");

            this.dbContext.Update<BackupRunFileRef>(backupRunFileRef);
            dbContext.SaveChanges();
        }


        public void Dispose()
        {
            if (null == dbContext)
            {
                this.Logger.LogWarning("No dbContext found to close");
            }
            else
            {
                this.Logger.LogInformation("Disposing dbContext");
                dbContext.Dispose();

               this.Logger.LogInformation("Closing dbConnection");
               dbConnection.Close();              
            }

            
        }

        public BackupRun GetBackupRun(int backupRunID) => this.dbContext.BackupRuns.Find(backupRunID);
        
        private ILogger Logger => this.serviceProvider.GetService<ILogger<SqliteDBHandler>>();
    }    
}
