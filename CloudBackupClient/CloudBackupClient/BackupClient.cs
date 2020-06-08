using CloudBackupClient.BackupClientController;
using CloudBackupClient.ClientDBHandlers;
using CloudBackupClient.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

namespace CloudBackupClient
{
    public class BackupClient
    {
        
        private readonly IClientDBHandler clientDBHandler;

        private readonly IBackupRunControl backupRunControl;

        private readonly IConfiguration configuration;

        private readonly ILogger<BackupClient> logger;

        public BackupClient(IClientDBHandler clientDBHandler,
                            IBackupRunControl backupRunControl,
                            IConfiguration configuration,
                            ILogger<BackupClient> logger)
        {            
            this.clientDBHandler = clientDBHandler;
            this.backupRunControl = backupRunControl;
            this.configuration = configuration;         
            this.logger = logger;
        }

        public async Task Start()
        {
            var configSection = this.configuration.GetSection(BackupClientConfigurationKeys.BackupSettings);
            var clientId = configSection[BackupClientConfigurationKeys.BackupClientID];

            this.logger.LogInformation($"Starting backup run at {DateTime.Now} for client ID {clientId}");
                                    
            BackupRun backupRun = null;

            try
            {
                backupRun = this.backupRunControl.GetNextBackupRun();                

                this.logger.LogInformation($"Processing open backup run with ID: {backupRun.BackupRunID} for client ID {clientId}");

                await this.backupRunControl.ArchiveBackupRunAsync(backupRun);

                this.logger.LogInformation($"Completed proccessing backup run with ID: {backupRun.BackupRunID} for client ID {clientId}");
            }
            catch (Exception ex)
            {
                if (backupRun == null)
                {
                    this.logger.LogError($"Couldn't complete processing due to error - {ex.Message} {ex.StackTrace}");
                }
                else
                {
                    this.logger.LogError($"Error in processing backup run with ID: {backupRun.BackupRunID} - error message: {ex.Message}  {ex.StackTrace}");

                    backupRun.BackupRunCompleted = true;
                    backupRun.BackupRunEnd = DateTime.Now;
                    backupRun.FailedWithException = true;
                    backupRun.ExceptionMessage = String.Format("{0}:{1}", ex.Message, ex.StackTrace);

                    try
                    {
                        this.clientDBHandler.UpdateBackupRun(backupRun);
                    }
                    catch (Exception dbEx)
                    {
                        this.logger.LogError($"Couldn't save backup run exception: {dbEx.Message}");

                        if (dbEx.InnerException != null)
                        {
                            this.logger.LogError($"Inner exception: {dbEx.InnerException.Message} - {dbEx.InnerException.StackTrace}");
                        }

                        throw;
                    }
                }

                if (ex.InnerException != null)
                {
                    this.logger.LogError($"Inner exception: {ex.InnerException.Message} - {ex.InnerException.StackTrace}");
                }

                throw;
            }
            finally
            {
                this.clientDBHandler.Dispose();
            }
        }
    }
}
