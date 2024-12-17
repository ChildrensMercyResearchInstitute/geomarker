
namespace GeoMarker.Frontiers.Core.Models.Commands
{
    public class GeocodedJsonResponse
    {
        public CommandStatus Status { get; set; } = CommandStatus.Unknown;
        public string Response { get; set; } = string.Empty;
    }
}
