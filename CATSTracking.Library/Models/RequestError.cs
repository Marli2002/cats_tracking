using System.ComponentModel.DataAnnotations;

namespace CATSTracking.Library.Models
{
    /// <summary>
    /// Returned when an API request fails. This is used to provide a consistent error response format.
    /// </summary>
    public class RequestError
    {
        /// <summary>
        /// A unique code that identifies the error. This code could be unique to the API or utilize an HTTP status code.
        /// </summary>
        [Required]
        public int Code { get; set; }

        /// <summary>
        /// A description of what went wrong minus techincal information.
        /// </summary>
        [Required]
        public string Description { get; set; }

        /// <summary>
        /// If public techinical information is available, it will be provided here.
        /// </summary>
        public string TechnicalInformation { get; set; }
    }
}
