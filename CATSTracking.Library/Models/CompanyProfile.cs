using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CATSTracking.Library.Models
{
    public class CompanyProfile
    {

        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }
		
        [Required]
        public string RegisteredName { get; set; }

        [Required]
        public string TradingName { get; set; }

		[Required]
        public string RegNo { get; set; }
		
		public string VatNo { get; set; }

		// NOTE: This account status type might change to int later on
		// Want to see how effective the string works for stuff like ACTIVE, SUSPENDED etc.
        [Required]
        public string AccountStatus { get; set; }

    }
}