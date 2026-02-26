using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CATSTracking.Library.Models
{
    public class UserProfile
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Required]
        public string FirstName { get; set; }
		
		[Required]
        public string LastName { get; set; }
		
        
        public int? CompanyId { get; set; }

        [ForeignKey("CompanyId")]
        [BindNever]
        public CompanyProfile? Company { get; set; }
		
		[Required]
        public string LoginId { get; set; }

        [ForeignKey("LoginId")]
        [BindNever]
        public IdentityUser Login { get; set; }
		
		[Required]
        public bool APIUser { get; set; }

    }
}
