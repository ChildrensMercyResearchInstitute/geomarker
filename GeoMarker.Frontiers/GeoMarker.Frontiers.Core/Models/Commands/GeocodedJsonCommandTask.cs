using Microsoft.AspNetCore.Http;

namespace GeoMarker.Frontiers.Core.Models.Commands
{
    public class GeocodedJsonCommandTask : CommandTask
    {
        public string Lat { get; set; } = string.Empty;
        public string Lon { get; set; } = string.Empty;
        public string? Site { get; set; }
        public int? Year { get; set; }
    }
}
