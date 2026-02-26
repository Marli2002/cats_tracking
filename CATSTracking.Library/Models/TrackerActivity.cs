using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CATSTracking.Library.Models
{
    public class TrackerActivity
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

		[Required]
        public DateTime UTCDateTime { get; set; }

		[Required]
        public int TrackerId { get; set; }

        [ForeignKey("TrackerId")]
        [BindNever]
        public Tracker TrackerEntity { get; set; }
	
        [Required]
        public string ActivityType { get; set; }
		
		[Required]
        public string Value{ get; set; }
		

    }
}