using System.ComponentModel.DataAnnotations;

namespace GeoMarker.Frontiers.Core.Models.Request
{
    public class DeGaussCompositeJsonRequest
    {
        [Required]
        public List<DeGaussAddressRequest> Addresses { get; set; } = new List<DeGaussAddressRequest>();
        public List<string> Services { get; set; } = new List<string>();
        public string Site { get; set; } = string.Empty;
        public int Year { get; set; }
    }
}
