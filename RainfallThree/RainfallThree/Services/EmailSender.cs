using Microsoft.AspNetCore.Identity.UI.Services;
using System.Net;
using System.Net.Mail;

namespace RainfallThree.Services
{
    public class EmailSender : IEmailSender
    {
        public Task SendEmailAsync(string email, string subject, string htmlMessage)
        {
            var client = new SmtpClient("smtp.gmail.com")
            {
                Port = 587,
                Credentials = new NetworkCredential("admin@edre.ethekwinifews.durban", "yxxy kywq vrde biyb"),
                EnableSsl = true,
            };

            var mail = new MailMessage
            {
                From = new MailAddress("admin@edre.ethekwinifews.durban"),
                Subject = subject,
                Body = htmlMessage,
                IsBodyHtml = true
            };

            mail.To.Add(email);

            return client.SendMailAsync(mail);
        }
    }
}
