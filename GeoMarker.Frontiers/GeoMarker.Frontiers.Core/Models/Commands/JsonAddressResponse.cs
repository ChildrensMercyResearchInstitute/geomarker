
namespace GeoMarker.Frontiers.Core.Models.Commands
{
    public class JsonAddressResponse
    {
        public CommandStatus Status { get; set; } = CommandStatus.Unknown;
        public string GeocodedAddress { get; set; } = string.Empty;
    }
}
