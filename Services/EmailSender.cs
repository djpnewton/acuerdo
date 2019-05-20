using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using System.Net.Mail;

namespace viafront3.Services
{
    // This class is used by the application to send email for account confirmation and password reset.
    // For more details see https://go.microsoft.com/fwlink/?LinkID=532713
    public class EmailSender : IEmailSender
    {
        readonly EmailSenderSettings _settings;

        public EmailSender(IOptions<EmailSenderSettings> optionsAccessor)
        {
            _settings = optionsAccessor.Value;
        }

        public Task SendEmailAsync(string email, string subject, string message)
        {
            return Execute(subject, message, email);
        }

        public Task Execute(string subject, string message, string email)
        { 
            // create smtp client
            var smtp = new SmtpClient();
            smtp.Host = _settings.SmtpHost;
            smtp.Port = _settings.SmtpPort;
            smtp.EnableSsl = _settings.SmtpSsl;
            if (_settings.SmtpUser != null)
                smtp.Credentials = new System.Net.NetworkCredential(_settings.SmtpUser, _settings.SmtpPass);
            // create mail
            var mail = new MailMessage();
            mail.From = new System.Net.Mail.MailAddress(_settings.From);
            mail.To.Add(new MailAddress(email));
            mail.Subject = subject;
            mail.IsBodyHtml = true;
            mail.Body = message;
            // send mail
            smtp.Send(mail);
            return Task.CompletedTask;
        }
    }
}
