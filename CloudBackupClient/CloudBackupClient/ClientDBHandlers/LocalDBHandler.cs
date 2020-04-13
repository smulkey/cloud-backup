using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;

namespace CloudBackupClient.ClientDBHandlers
{
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



        //var openBackupRuns = dbContext.BackupRuns.Where<BackupRun>(b => b.BackupRunCompleted == false).ToList<BackupRun>();

        //dbContext.Add<BackupRun>(br);
        //dbContext.SaveChanges();


        /*
         * 
         * dbContext.Update<BackupRunFileRef>(item); 
          catch (Exception ex)
                                {
                                    br.FailedWithException = true;
                                    br.BackupRunEnd = DateTime.Now;
                                    br.ExceptionMessage = ex.Message;

                                    dbContext.Update<BackupRun>(br);

                                    throw new Exception(String.Format("Backup run with ID {0} failed with exception: {1}", br.BackupRunID, ex.Message));
                                }
                                finally
                                {
                                    dbContext.SaveChanges();
                                }
                 */
    //}
}
