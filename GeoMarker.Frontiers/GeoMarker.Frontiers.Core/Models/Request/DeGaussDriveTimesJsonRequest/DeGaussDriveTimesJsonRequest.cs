using System.ComponentModel.DataAnnotations;

namespace GeoMarker.Frontiers.Core.Models.Request
{
    public class DeGaussDriveTimesJsonRequest : DeGaussGeocodedJsonRequest
    {
        [Required]
        public string Site { get; set; } = string.Empty;
    }
}
