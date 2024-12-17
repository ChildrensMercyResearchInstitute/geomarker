using GeoMarker.Frontiers.Core.Models.Request;

namespace GeoMarker.Frontiers.Core.Models.Commands
{
    public class JsonAddressCommandTask : CommandTask
    {
        public List<DeGaussAddressRequest> Addresses { get; set; } = new();
    }
}
