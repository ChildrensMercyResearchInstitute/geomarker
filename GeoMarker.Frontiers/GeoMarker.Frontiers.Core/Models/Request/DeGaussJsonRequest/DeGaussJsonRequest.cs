using System.ComponentModel.DataAnnotations;

namespace GeoMarker.Frontiers.Core.Models.Request
{
    public class DeGaussJsonRequest
    {
        [Required]
        public List<DeGaussAddressRequest> Addresses { get; set; } = new List<DeGaussAddressRequest>();
    }

    public class DeGaussAddressRequest
    {
        public string Id { get; set; }
        public string Address { get; set; }
    }
}
