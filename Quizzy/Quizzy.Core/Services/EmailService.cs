using Quizzy.Core.Entities;
using System.Net;
using System.Net.Mail;

namespace Quizzy.Core.Services
{
    public class EmailService : IEmailService
    {
        static bool SendEmailsEnabled = true; // Set to false to disable email sending because i dont want to be spammed and there's a limit

        public async Task SendEmailAsync(UserAccount reciever, string subject, string body, string[]? attachments = null)
        {
            string formattedBody = CreateEmailBody(body);

            using var message = new MailMessage
            {
                From = new MailAddress("quizzyquizapp@gmail.com", "Quizzy"),
                Subject = subject,
                Body = formattedBody,
                IsBodyHtml = true
            };
            message.To.Add(reciever.Email);

            if (attachments is { Length: > 0 })
            {
                foreach (var path in attachments) message.Attachments.Add(new Attachment(path));
            }

            if (SendEmailsEnabled) await SendEmail(message);
        }

        string CreateEmailBody(string body)
        {
            string header = // This is really important trust me
            "<h1 style=\"margin:0;font-family:Arial,Helvetica,sans-serif;font-size:28px;line-height:1.2;\">" +
                "<font color=\"#e53935\" style=\"color:#ff2b2b;\">Q</font>" +
                "<font color=\"#fb8c00\" style=\"color:#ffe600;\">u</font>" +
                "<font color=\"#fdd835\" style=\"color:#fff200;\">i</font>" +
                "<font color=\"#43a047\" style=\"color:#00ff99;\">z</font>" +
                "<font color=\"#1e88e5\" style=\"color:#0800ff;\">z</font>" +
                "<font color=\"#8e24aa\" style=\"color:#ff00e1;\">y</font>" +
            "</h1>";
            string footer = "<p style=\"margin:5rem;\">Best regards,<br/>The Omnipotent Quizzy Team</p>";
            return $"<html><body>{header}<br /><div style=\"color:#000000;\"><p>{body}</p><br />{footer}</div></body></html>";
        }

        private async Task SendEmail(MailMessage message)
        {
            var smtp = new SmtpClient("smtp.gmail.com", 587)
            {
                EnableSsl = true,
                DeliveryMethod = SmtpDeliveryMethod.Network,
                UseDefaultCredentials = false,
                Credentials = new NetworkCredential("quizzyquizapp@gmail.com", "ejrv qayc iqat kpoi")
            };

            await smtp.SendMailAsync(message);
            smtp.Dispose();
        }
    }
}