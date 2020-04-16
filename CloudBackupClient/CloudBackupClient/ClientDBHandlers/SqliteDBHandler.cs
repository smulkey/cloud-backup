using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Data.Sqlite;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using CloudBackupClient.Data;
using Microsoft.Extensions.Logging;
using CloudBackupClient.Models;
using System.Data.Common;

namespace CloudBackupClient.ClientDBHandlers
{
    public class SqliteDBHandler : IClientDBHandler
    {
        public string ConnectionString { get; private set; }
        private DbConnection dbConnection;
        private CloudBackupDbContext dbContext;
        private IServiceProvider serviceProvider;
        
        public SqliteDBHandler(IServiceProvider serviceProvider)
        {
            this.serviceProvider = serviceProvider;           
        }

        public void Initialize(IDictionary<string, string> dbProperties)
        {
            //TODO Add validation
            this.ConnectionString = dbProperties[nameof(this.ConnectionString)];

            dbConnection = new SqliteConnection(this.ConnectionString);
            dbConnection.Open();

            var options = new DbContextOptionsBuilder<CloudBackupDbContext>()
                .UseSqlite(dbConnection)
                .Options;

            // Create the schema in the database
            dbContext = new CloudBackupDbContext(options);
            dbContext.Database.EnsureCreated();

            var count = dbContext.BackupRuns.Count<BackupRun>();

            Logger.LogInformation(count.ToString());
        }

        public IList<BackupRun> GetOpenBackupRuns()
        {
            var openBackupRuns = this.dbContext.BackupRuns.Where(b => b.BackupRunCompleted == false).ToList<BackupRun>();

            this.Logger.LogInformation($"Returning BackupRun set with {openBackupRuns.Count} entries");

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

        public void UpdateBackupDirectoryRef(BackupDirectoryRef backupDirectoryRef)
        {
            this.Logger.LogInformation($"Updating BackupDirectoryRef set with DirectoryRefID: {backupDirectoryRef.DirectoryRefID}");

            this.dbContext.Update<BackupDirectoryRef>(backupDirectoryRef);
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

        private ILogger Logger => this.serviceProvider.GetService<ILogger<SqliteDBHandler>>();
    }
    //public class LocalDBHandler : IClientDBHandler
    //{
        /*
         *  var connection = new SqliteConnection(BackupClient.AppConfig.GetConnectionString("BackupRunConnStr"));

            try
            {
                connection.Open();

                var options = new DbContextOptionsBuilder<CloudBackupDbContext>()
                    .UseSqlite(connection)
                    .Options;

         using (var dbContext = new CloudBackupDbContext(options))
                {
                    BackupRun br = null;

                    try
                    {
                        dbContext.Database.EnsureCreated();

                      
                    }
                    catch (Exception ex)
                    {
                        
                            try
                            {
                                dbContext.SaveChanges();
                            }
                            catch (Exception dbEx)
                            {
                                this.Logger.LogError(dbEx, "Couldn't save backup run exception: {0}", dbEx.Message);
                            }

                        }
                        
                    }
                }

        // connection.Close();
                    */


       


       
    //}
}
