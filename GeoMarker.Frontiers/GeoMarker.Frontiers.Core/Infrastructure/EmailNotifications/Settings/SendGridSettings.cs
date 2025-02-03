using Microsoft.Extensions.Options;

namespace GeoMarker.Frontiers.Core.Infrastructure.EmailNotifications.Settings
{
    public interface ISendGridSettings : IEmailSettings
    {
        public string ApiKey { get; set; }
    }

    public class SendGridSettings : EmailSettings, ISendGridSettings
    {
        public string ApiKey { get; set; } = string.Empty;
    }

    public class SendGridSettingsValidation : IValidateOptions<SendGridSettings>
    {
        public ValidateOptionsResult Validate(string name, SendGridSettings options)
        {
            if (string.IsNullOrEmpty(options.FromEmail))
            {
                return ValidateOptionsResult.Fail($"{nameof(options.FromEmail)} configuration parameter for email settings is required");
            }

            if (string.IsNullOrEmpty(options.ApiKey))
            {
                return ValidateOptionsResult.Fail($"{nameof(options.ApiKey)} configuration parameter for email settings is required");
            }

            return ValidateOptionsResult.Success;
        }
    }
}
