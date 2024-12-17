using GeoMarker.Frontiers.Core.Models.Commands;

namespace GeoMarker.Frontiers.Core.Models.Response
{
    public class DeGaussAsyncResponse
    {
        public CommandStatus Status { get; set; }
        public string Guid { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
    }
}
