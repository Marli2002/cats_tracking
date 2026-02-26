using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CATSTracking.Library.Models
{
    public class UserTracker
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }
		
		[Required]
        public string LoginId { get; set; }

        [ForeignKey("LoginId")]
        [BindNever]
        public IdentityUser Login { get; set; }
		
		[Required]
        public int TrackerId { get; set; }

        [ForeignKey("TrackerId")]
        [BindNever]
        public Tracker TrackerObj { get; set; }
		
		[Required]
        public bool TrackerAdmin { get; set; }

    }
}