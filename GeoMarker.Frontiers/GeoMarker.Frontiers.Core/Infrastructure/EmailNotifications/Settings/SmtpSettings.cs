using Microsoft.Extensions.Options;

namespace GeoMarker.Frontiers.Core.Infrastructure.EmailNotifications.Settings
{
    public interface ISmtpSettings : IEmailSettings
    {
        public string Host { get; set; }
        public int Port { get; set; }
        public string Username { get; set; }
        public string Password { get; set; }
    }

    public class SmtpSettings : EmailSettings, ISmtpSettings
    {
        public string Host { get; set; } = string.Empty;
        public int Port { get; set; } = 0;
        public string Username { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }

    public class SmtpSettingsValidation : IValidateOptions<SmtpSettings>
    {
        public ValidateOptionsResult Validate(string name, SmtpSettings options)
        {
            if (string.IsNullOrEmpty(options.FromEmail))
            {
                return ValidateOptionsResult.Fail($"{nameof(options.FromEmail)} configuration parameter for email settings is required");
            }

            if (string.IsNullOrEmpty(options.Host))
            {
                return ValidateOptionsResult.Fail($"{nameof(options.Host)} configuration parameter for email settings is required");
            }

            if (options.Port <= 0)
            {
                return ValidateOptionsResult.Fail($"{nameof(options.Host)} configuration parameter for email settings is required");
            }

            return ValidateOptionsResult.Success;
        }
    }
}
