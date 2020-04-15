using CloudBackupClient.Data;
using CloudBackupClient.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;

namespace CloudBackupClient.ClientDBHandlers
{
    public class InMemoryDBHandler : IClientDBHandler
    {        
        private CloudBackupDbContext dbContext;       
        private IServiceProvider serviceProvider;
                
        public InMemoryDBHandler(IServiceProvider serviceProvider) 
        {
            this.serviceProvider = serviceProvider;

            var options = new DbContextOptionsBuilder<CloudBackupDbContext>()
                .UseInMemoryDatabase(databaseName: "BackupClientDatabase")
                .Options;
                        
            // Create the schema in the database
            dbContext = new CloudBackupDbContext(options);                
            dbContext.Database.EnsureCreated();            
        }

        public void AddBackupRun(BackupRun br)
        {
            this.Logger.LogInformation("Adding BackupRun with {0} file refs", br.BackupFileRefs == null ? 0 : br.BackupFileRefs.Count);

            this.dbContext.Add<BackupRun>(br);
            this.dbContext.SaveChangesAsync();            
        }

        public IList<BackupRun> GetOpenBackupRuns()
        {
            var openBackupRuns = this.dbContext.BackupRuns.Where(b => b.BackupRunCompleted == false).ToList<BackupRun>();

            this.Logger.LogInformation("Returning BackupRun set with {0} entries", openBackupRuns.Count);

            return openBackupRuns;
        }

        public void UpdateBackupFileRef(BackupRunFileRef item)
        {
            this.Logger.LogInformation("Updating BackupRunFileRef set with BackupRunFileRefID: {0}", item.BackupRunFileRefID);

            this.dbContext.Update<BackupRunFileRef>(item);
            this.dbContext.SaveChangesAsync();
        }

        public void UpdateBackupRun(BackupRun br)
        {
            this.Logger.LogInformation("Updating BackupRun set with BackupRunID: {0}", br.BackupRunID);

            this.dbContext.Update<BackupRun>(br);
            this.dbContext.SaveChangesAsync();
        }

        public void Dispose()
        {
            if( null == dbContext )
            {
                this.Logger.LogWarning("No dbContext found to close");
            }
            else
            {
                this.Logger.LogInformation("Disposing dbContext");
                dbContext.Dispose();
            }            
        }

        public void Initialize(IDictionary<string, string> dbProperties)
        {
            throw new NotImplementedException();
        }

        private ILogger Logger => this.serviceProvider.GetService<ILogger<InMemoryDBHandler>>();
    }
}
