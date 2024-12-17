using Microsoft.AspNetCore.Http;

namespace GeoMarker.Frontiers.Core.Models.Commands
{
    public class DeGaussCommandTask : CommandTask
    {
        public IFormFile? File { get; set; }
        public string? Site { get; set; }
        public int? Year { get; set; }
        public string Matcher { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
    }
}
