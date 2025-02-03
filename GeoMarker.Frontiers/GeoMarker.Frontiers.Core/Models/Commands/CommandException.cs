
namespace GeoMarker.Frontiers.Core.Models.Commands
{
    public class CommandException : Exception
    {
        public CommandException(string message) : base(message)
        {
        }

        public CommandException(string message, Exception inner) : base(message, inner)
        {
        }
    }
}
