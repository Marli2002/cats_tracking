using System.ComponentModel.DataAnnotations;

namespace CATSTracking.Library.Models
{
    public class TokenRequest
    {
        /// <summary>
        /// The username used when logging into the panel via the web interface
        /// </summary>
        [Required]
        public string Username { get; set; }

        /// <summary>
        /// The password used when logging into the panel via the web interface
        /// </summary>
        [Required]
        public string Password { get; set; }

        /// <summary>
        /// OPTIONAL: A comma-separated list of grants requested by the client
        /// </summary>
        public string GrantsRequested { get; set; }
    }
}
