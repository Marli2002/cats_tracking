using System.ComponentModel.DataAnnotations;

namespace CATSTracking.Library.Models
{
    public class Login
    {

        [Required]
        public string Username { get; set; }

        [Required]
        public string? Password { get; set; }

        public string? ResetToken { get; set; }

    }
}
