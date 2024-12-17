
namespace GeoMarker.Frontiers.Core.Models.Commands
{
    public class CommandTaskResponse
    {
        public CommandStatus Status { get; set; } = CommandStatus.Unknown;

        public MemoryStream? Stream { get; set; }
    }
}
