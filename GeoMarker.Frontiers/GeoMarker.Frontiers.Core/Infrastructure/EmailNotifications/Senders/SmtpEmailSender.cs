using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Identity.UI.Services;
using GeoMarker.Frontiers.Core.Infrastructure.EmailNotifications.Settings;
using System.Net.Mail;
using System.Net;
using System.Text;

namespace GeoMarker.Frontiers.Core.Infrastructure.EmailNotifications.Senders
{
    public class SmtpEmailSender : IEmailSender
    {
        private readonly ISmtpSettings _emailSettings;
        private readonly ILogger<SmtpEmailSender> _logger;

        public SmtpEmailSender(ISmtpSettings emailSettings, ILogger<SmtpEmailSender> logger)
        {
            _emailSettings = emailSettings;
            _logger = logger;
        }

        public async Task SendEmailAsync(string ToEmail, string Subject, string Content)
        {
            try
            {
                SmtpClient client = new SmtpClient(_emailSettings.Host, _emailSettings.Port);
                client.EnableSsl = true;
                if (!string.IsNullOrEmpty(_emailSettings.Username) && !string.IsNullOrEmpty(_emailSettings.Password))
                    client.Credentials = new NetworkCredential(_emailSettings.Username, _emailSettings.Password);

                var from = string.IsNullOrEmpty(_emailSettings.FromName) ?
                    new MailAddress(_emailSettings.FromEmail) :
                    new MailAddress(_emailSettings.FromEmail, _emailSettings.FromName);
                var msg = new MailMessage(from, new MailAddress(ToEmail));
                msg.IsBodyHtml = true;
                msg.Body = Content;
                msg.Subject = Subject;
                msg.SubjectEncoding = Encoding.UTF8;

                client.Send(msg);
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
