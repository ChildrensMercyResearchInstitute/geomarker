using GeoMarker.Frontiers.Core.Infrastructure.EmailNotifications.Senders;
using GeoMarker.Frontiers.Core.Infrastructure.EmailNotifications.Settings;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace GeoMarker.Frontiers.Core.Infrastructure.EmailNotifications
{
    public static class EmailDependency
    {
        private static readonly string[] _supportedTypes = new string[] { "Smtp", "SendGrid" };
        private static readonly string _notSupportedMessage = "Email Service Type {0} is not supported. Supported types: " + _supportedTypes.Aggregate("", (acc, next) => acc += ", " + next);
        private static readonly string _requiredMessage = "Email Service Type is required";

        public static IServiceCollection AddEmailServices(this IServiceCollection services, IConfiguration configuration)
        {
            var type = configuration.GetValue<string>("EmailServiceType");
            if (string.IsNullOrEmpty(type))
                throw new ArgumentNullException(_requiredMessage);
            else if (string.Equals(type, "Smtp", StringComparison.OrdinalIgnoreCase))
                services.AddTransient<IEmailSender, SmtpEmailSender>();
            else if (string.Equals(type, "SendGrid", StringComparison.OrdinalIgnoreCase))
                services.AddTransient<IEmailSender, SendGridEmailSender>();
            else
                throw new NotSupportedException(string.Format(_notSupportedMessage, type));
            return services;
        }

        public static IServiceCollection AddEmailSetting(this IServiceCollection services, IConfiguration configuration)
        {
            var type = configuration.GetValue<string>("EmailServiceType");
            if (string.IsNullOrEmpty(type))
                throw new ArgumentNullException(_requiredMessage);
            else if (string.Equals(type, "Smtp", StringComparison.OrdinalIgnoreCase))
            {
                var settings = new SmtpSettings();
                settings.FromEmail = configuration.GetValue<string>("EmailServiceFromEmail");
                settings.FromName = configuration.GetValue<string>("EmailServiceFromName");
                settings.Host = configuration.GetValue<string>("EmailServiceHost");
                settings.Port = configuration.GetValue<int>("EmailServicePort");
                settings.Username = configuration.GetValue<string>("EmailServiceUsername");
                settings.Password = configuration.GetValue<string>("EmailServicePassword");

                services.AddSingleton<IValidateOptions<SmtpSettings>, SmtpSettingsValidation>();
                services.AddSingleton<ISmtpSettings>(settings);
            }
            else if (string.Equals(type, "SendGrid", StringComparison.OrdinalIgnoreCase))
            {
                var settings = new SendGridSettings();
                settings.FromEmail = configuration.GetValue<string>("EmailServiceFromEmail");
                settings.FromName = configuration.GetValue<string>("EmailServiceFromName");
                settings.ApiKey = configuration.GetValue<string>("EmailServiceApiKey");

                services.AddSingleton<IValidateOptions<SendGridSettings>, SendGridSettingsValidation>();
                services.AddSingleton<ISendGridSettings>(settings);
            }
            else
                throw new NotSupportedException(string.Format(_notSupportedMessage, type));
            return services;
        }
    }
}
