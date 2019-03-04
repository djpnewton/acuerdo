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
            return Execute(_settings.SmtpHost, _settings.From, subject, message, email);
        }

        public Task Execute(string smtpHost, string from, string subject, string message, string email)
        { 
            // create smtp client
            var smtp = new SmtpClient();
            smtp.Host = smtpHost;
            // create mail
            var mail = new MailMessage();
            mail.From = new System.Net.Mail.MailAddress("from");
            mail.To.Add(new MailAddress(email));
            //mail.IsBodyHtml = true;
            mail.Body = message;
            // send mail
            smtp.Send(mail);
            return Task.CompletedTask;
        }
    }
}
