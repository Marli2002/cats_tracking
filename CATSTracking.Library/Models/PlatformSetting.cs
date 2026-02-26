using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CATSTracking.Library.Models
{
    public class PlatformSetting
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }
		
        [Required]
        public string Key { get; set; }

        [Required]
        public string Value { get; set; }

        [Required]
        public string Type { get; set; }

        [Required]
        public bool CanChange { get; set; }

        [Required]
        public DateTime UTCLastSet { get; set; }
		
		[Required]
        public string LastSetByUserId { get; set; }

        [ForeignKey("LastSetByUserId")]
        [BindNever]
        public IdentityUser Login { get; set; }

    }
}