using MailKit.Net.Smtp;
using MimeKit;

namespace PETHUB.Services
{
    public class EmailSender
    {
        private readonly IConfiguration _config;

        public EmailSender(IConfiguration config)
        {
            _config = config;
        }

        public async Task SendEmailAsync(string toEmail, string subject, string body)
        {
            var email = new MimeMessage();
            email.From.Add(new MailboxAddress(
                _config["MailSettings:SenderName"],
                _config["MailSettings:SenderEmail"]));
            email.To.Add(new MailboxAddress("", toEmail));
            email.Subject = subject;
            email.Body = new TextPart("html") { Text = body };

            using var smtp = new SmtpClient();
            await smtp.ConnectAsync(_config["MailSettings:Server"], int.Parse(_config["MailSettings:Port"]), false);
            await smtp.AuthenticateAsync(_config["MailSettings:UserName"], _config["MailSettings:Password"]);
            await smtp.SendAsync(email);
            await smtp.DisconnectAsync(true);
        }
    }
}

