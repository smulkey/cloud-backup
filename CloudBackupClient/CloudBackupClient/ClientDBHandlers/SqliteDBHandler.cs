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
        private readonly DbConnection dbConnection;
        private readonly CloudBackupDbContext dbContext;
                            
        private readonly ILogger<SqliteDBHandler> logger;

        private const string ConnectionStringKey = "SqliteConnString";

        public  SqliteDBHandler(IConfiguration configuration, ILogger<SqliteDBHandler> logger)
        {          
            this.logger = logger;

            var connString = configuration.GetConnectionString(ConnectionStringKey);

            this.logger.LogInformation("Initializing DB handler");

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

            this.logger.LogInformation($"Returning BackupRun set with {openBackupRuns.Count} entries");

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
            this.logger.LogInformation("Adding BackupRun with {0} file refs", backupRun.BackupFileRefs == null ? 0 : backupRun.BackupFileRefs.Count);

            this.dbContext.Add<BackupRun>(backupRun);
            dbContext.SaveChanges();
        }

        public void UpdateBackupRun(BackupRun backupRun)
        {            
            this.logger.LogInformation($"Updating BackupRun set with BackupRunID: {backupRun.BackupRunID}");

            this.dbContext.Update<BackupRun>(backupRun);
            dbContext.SaveChanges();
        }
              
        public void UpdateBackupFileRef(BackupRunFileRef backupRunFileRef)
        {
            this.logger.LogInformation($"Updating BackupRunFileRef set with BackupRunFileRefID: {backupRunFileRef.BackupRunFileRefID}");

            this.dbContext.Update<BackupRunFileRef>(backupRunFileRef);
            dbContext.SaveChanges();
        }


        public void Dispose()
        {
            if (null == dbContext)
            {
                this.logger.LogWarning("No dbContext found to close");
            }
            else
            {
                this.logger.LogInformation("Disposing dbContext");
                dbContext.Dispose();

               this.logger.LogInformation("Closing dbConnection");
               dbConnection.Close();              
            }

            
        }

        public BackupRun GetBackupRun(int backupRunID) => this.dbContext.BackupRuns.Find(backupRunID);
    }    
}
