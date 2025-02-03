using Microsoft.Extensions.Options;

namespace GeoMarker.Frontiers.Core.Infrastructure.EmailNotifications.Settings
{
    public class EmailSettings : IEmailSettings
    {
        public string FromEmail { get; set; } = string.Empty;
        public string FromName { get; set; } = string.Empty;
    }

    public interface IEmailSettings
    {
        public string FromEmail { get; set; }
        public string FromName { get; set; }
    }
}
