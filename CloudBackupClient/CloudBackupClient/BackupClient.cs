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
        private readonly IServiceProvider serviceProvider;

        public BackupClient(IServiceProvider serviceProvider)
        {            
            this.serviceProvider = serviceProvider;           
        }

        public async Task Start()
        {
            var clientId = this.serviceProvider.GetService<IConfigurationRoot>().GetSection(BackupClientConfigurationKeys.BackupSettings).GetSection(BackupClientConfigurationKeys.BackupClientID).Value;

            this.Logger.LogInformation($"Starting backup run at {DateTime.Now} for client ID {clientId}");
                                    
            BackupRun backupRun = null;

            try
            {
                backupRun = this.BackupRunControl.GetNextBackupRun();                

                this.Logger.LogInformation($"Processing open backup run with ID: {backupRun.BackupRunID} for client ID {clientId}");

                await this.BackupRunControl.ArchiveBackupRunAsync(backupRun);

                this.Logger.LogInformation($"Completed proccessing backup run with ID: {backupRun.BackupRunID} for client ID {clientId}");
            }
            catch (Exception ex)
            {
                if (backupRun == null)
                {
                    this.Logger.LogError($"Couldn't complete processing due to error - {ex.Message} {ex.StackTrace}");
                }
                else
                {
                    this.Logger.LogError($"Error in processing backup run with ID: {backupRun.BackupRunID} - error message: {ex.Message}  {ex.StackTrace}");

                    backupRun.BackupRunCompleted = true;
                    backupRun.BackupRunEnd = DateTime.Now;
                    backupRun.FailedWithException = true;
                    backupRun.ExceptionMessage = String.Format("{0}:{1}", ex.Message, ex.StackTrace);

                    try
                    {
                        this.ClientDBHandler.UpdateBackupRun(backupRun);
                    }
                    catch (Exception dbEx)
                    {
                        this.Logger.LogError($"Couldn't save backup run exception: {dbEx.Message}");

                        if (dbEx.InnerException != null)
                        {
                            this.Logger.LogError($"Inner exception: {dbEx.InnerException.Message} - {dbEx.InnerException.StackTrace}");
                        }

                        throw;
                    }
                }

                if (ex.InnerException != null)
                {
                    this.Logger.LogError($"Inner exception: {ex.InnerException.Message} - {ex.InnerException.StackTrace}");
                }

                throw;
            }
            finally
            {
                this.ClientDBHandler.Dispose();
            }
        }

        private IBackupRunControl BackupRunControl => this.serviceProvider.GetService<IBackupRunControl>();

        private IClientDBHandler ClientDBHandler => this.serviceProvider.GetService<IClientDBHandler>();
                
        private ILogger Logger => this.serviceProvider.GetService<ILogger<Program>>();
    }
}
