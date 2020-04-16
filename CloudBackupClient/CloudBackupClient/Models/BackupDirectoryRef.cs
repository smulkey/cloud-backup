using System;
using System.Collections.Generic;
using System.Text;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CloudBackupClient.Models
{
    public class BackupDirectoryRef
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int DirectoryRefID { get; set; }

        [ForeignKey("BackupRun")]
        public int BackupRunID { get; set; }

        public string DirectoryFullFileName { get; set; }
    }
}
