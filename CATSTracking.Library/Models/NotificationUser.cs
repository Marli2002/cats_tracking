using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CATSTracking.Library.Models
{
    public class NotificationUser
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }
		
		[Required]
        public int NotificationId { get; set; }

        [ForeignKey("NotificationId")]
        [BindNever]
        public Notification NotificationEntity { get; set; }
		
		[Required]
        public string LoginId { get; set; }

        [ForeignKey("LoginId")]
        [BindNever]
        public IdentityUser Login { get; set; }
		

    }
}