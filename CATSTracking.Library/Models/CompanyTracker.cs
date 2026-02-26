using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CATSTracking.Library.Models
{
    public class CompanyTracker
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }
		
		[Required]
        public int CompanyId { get; set; }

        [ForeignKey("CompanyId")]
        [BindNever]
        public CompanyProfile Company { get; set; }
		
		[Required]
        public int TrackerId { get; set; }

        [ForeignKey("TrackerId")]
        [BindNever]
        public Tracker TrackerEntity { get; set; }
		
		[Required]
        public bool AllUsers { get; set; }

    }
}