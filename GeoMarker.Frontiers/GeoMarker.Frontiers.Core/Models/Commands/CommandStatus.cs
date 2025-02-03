
namespace GeoMarker.Frontiers.Core.Models.Commands
{
    public enum CommandStatus
    {
        Processing,
        Rejected,
        Success,
        Failure,
        Duplicate,
        Queued,
        Removed,
        Requested,
        Unknown = 999      

    }
}
