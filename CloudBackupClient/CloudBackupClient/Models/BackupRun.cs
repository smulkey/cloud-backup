using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;

namespace CloudBackupClient.Models
{
    public class BackupRun
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int BackupRunID { get; set;  }

        public List<BackupRunFileRef> BackupFileRefs { get; set; }

        public DateTime BackupRunStart { get; set; }

        public DateTime BackupRunEnd { get; set; }

        public List<BackupDirectoryRef> BackupDirectories { get; set; }

        public bool BackupRunCompleted { get; set; }

        public bool FailedWithException { get; set; }

        public string ExceptionMessage { get; set; }
    }
}
