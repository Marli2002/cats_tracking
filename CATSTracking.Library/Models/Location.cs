using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CATSTracking.Library.Models
{
    public class Location
    {
        public string IMEI { get; set; }
        public double DecimalLatitude { get; set; }
        public double DecimalLongitude { get; set; }
        public string RawLatitude { get; set; }
        public string RawLongitude { get; set; }
        public string OSM_Link { get; set; }
        public string Timestamp { get; set; }
    }
}