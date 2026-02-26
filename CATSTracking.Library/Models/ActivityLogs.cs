using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CATSTracking.Library.Models
{
    [Table("ActivityLogs")]
    public class ActivityLog
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Required]
        [MaxLength(100)]
        public string Action { get; set; }

        [Required]
        [MaxLength(500)]
        public string Details { get; set; }

        [Required]
        [MaxLength(100)]
        public string PerformedBy { get; set; }

        [Required]
        public DateTime Timestamp { get; set; }
    }
}