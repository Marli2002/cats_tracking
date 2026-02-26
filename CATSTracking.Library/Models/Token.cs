using System.ComponentModel.DataAnnotations;

namespace CATSTracking.Library.Models
{
    public class Token
    {
        [Required]
        public string JWT { get; set; }

        public DateTime Expires { get; set; }

        public string UserName { get; set; }
        public string Grants { get; set; }
        

    }
}
