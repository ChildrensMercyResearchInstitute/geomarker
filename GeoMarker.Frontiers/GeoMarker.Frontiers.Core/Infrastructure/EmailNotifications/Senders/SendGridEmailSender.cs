using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Identity.UI.Services;
using SendGrid;
using SendGrid.Helpers.Mail;
using GeoMarker.Frontiers.Core.Infrastructure.EmailNotifications.Settings;

namespace GeoMarker.Frontiers.Core.Infrastructure.EmailNotifications.Senders
{
    public class SendGridEmailSender : IEmailSender
    {
        private readonly ISendGridSettings _emailSettings;
        private readonly ILogger<SendGridEmailSender> _logger;

        public SendGridEmailSender(ISendGridSettings emailSettings, ILogger<SendGridEmailSender> logger)
        {
            _emailSettings = emailSettings;
            _logger = logger;
        }

        public async Task SendEmailAsync(string ToEmail, string Subject, string HtmlContent)
        {
            try
            {
                var client = new SendGridClient(_emailSettings.ApiKey);
                var from = string.IsNullOrEmpty(_emailSettings.FromName) ?
                    new EmailAddress(_emailSettings.FromEmail) :
                    new EmailAddress(_emailSettings.FromEmail, _emailSettings.FromName);
                var msg = MailHelper.CreateSingleEmail(from, new EmailAddress(ToEmail), Subject, HtmlContent, HtmlContent);

                await client.SendEmailAsync(msg).ConfigureAwait(false);
                _logger.LogInformation($"Sent email");
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Error sending an email");
                throw;
            }
        }
    }
}
