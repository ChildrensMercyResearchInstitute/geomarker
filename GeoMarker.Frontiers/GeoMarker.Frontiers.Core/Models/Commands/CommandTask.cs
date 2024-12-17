using System.Diagnostics;

namespace GeoMarker.Frontiers.Core.Models.Commands
{
    public class CommandTask
    {
        public string CommandGuid { get; set; } = string.Empty;
        public string Command { get; set; } = string.Empty;
        public Process? Process { get; set; }
        public CommandStatus Status { get; set; } = CommandStatus.Unknown;
        public string StandardOut { get; set; } = string.Empty;
        public string StandardErr { get; set; } = string.Empty;
    }
}
