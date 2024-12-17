using System.ComponentModel.DataAnnotations;

namespace GeoMarker.Frontiers.Core.Models.Request
{
    public class DeGaussCensusBlockGroupsJsonRequest : DeGaussGeocodedJsonRequest
    {
        [Required]
        public int Year { get; set; }
    }
}
