using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CloudBackupClient.Models
{
    public class BackupDirectoryRef
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int DirectoryRefID { get; set; }

        [ForeignKey(nameof(BackupRun))]
        public int BackupRunID { get; set; }

        public string DirectoryFullFileName { get; set; }
    }
}
