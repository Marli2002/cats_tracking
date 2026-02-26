using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CATSTracking.Library.Models
{
    public class Tracker
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

	    [Required]
        public string IMEI { get; set; }
		
		[Required]
        public string SerialNo { get; set; }
		
		[Required]
        public string PhoneNumber { get; set; }

		[Required]
        public string DisplayName { get; set; }

		[Required]
        public bool Enabled { get; set; }

		[Required]
        public DateTime UTCLastSet { get; set; }

		[Required]
        public string AddedByLoginId { get; set; }

        [ForeignKey("AddedByLoginId")]
        [BindNever]
        public IdentityUser Login { get; set; }

    }
}