using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CATSTracking.Library.Models
{
    public class SMS
    {

        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        /// <summary>
        /// The from phone number where the SMS has come from.
        /// </summary>
        [Required]
        public string From { get; set; }

        /// <summary>
        /// The phone number the SMS is being sent to.
        /// </summary>
        [Required]
        public string To { get; set; }

        /// <summary>
        /// An encoded message body of the SMS message.
        /// </summary>
        [Required]
        public string Message { get; set; }
    }
}
