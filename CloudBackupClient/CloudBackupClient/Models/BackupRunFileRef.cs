using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CloudBackupClient.Models
{
    public class BackupRunFileRef
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int BackupRunFileRefID { get; set;  }

        [ForeignKey(nameof(BackupRun))]
        public int BackupRunID { get; set; }

        public String FullFileName { get; set; }

        public bool CopiedToCache { get; set; }

        public bool CopiedToArchive { get; set; }
    }
}
