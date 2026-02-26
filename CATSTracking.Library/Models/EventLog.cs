using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CATSTracking.Library.Models
{
    public class EventLog
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }
		
        [Required]
        public string Tag { get; set; }

        [Required]
        public string Message { get; set; }

        [Required]
        public DateTime UTCDateTime { get; set; }
		
        public string? LoginId { get; set; }

        [ForeignKey("LoginId")]
        [BindNever]
        public IdentityUser LoginEntity { get; set; }

    }
}