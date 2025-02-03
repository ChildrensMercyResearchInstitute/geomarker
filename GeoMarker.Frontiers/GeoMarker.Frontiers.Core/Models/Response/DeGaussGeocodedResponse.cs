using GeoMarker.Frontiers.Core.Models.Request;

namespace GeoMarker.Frontiers.Core.Models.Response
{
    public class DeGaussGeocodedResponse
    {
        public List<DeGaussGeocodedJsonRecord> Records { get; set; } = new List<DeGaussGeocodedJsonRecord>();
        public string? Site { get; set; }
        public string? Year { get; set; }
    }
}
