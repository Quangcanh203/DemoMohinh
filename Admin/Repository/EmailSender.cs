using System.Net.Mail;
using System.Net;

namespace DoAn.Areas.Admin.Repository
{
    public class EmailSender : IEmailSender
    {
        public Task SendEmailAsync(string email, string subject, string message)
        {
            var client = new SmtpClient("smtp.gmail.com", 587)
            {
                EnableSsl = true, //bật bảo mật
                UseDefaultCredentials = false,
                Credentials = new NetworkCredential("2100005423@nttu.edu.vn", "baxgzadrbnkbcyrc")
            };

            return client.SendMailAsync(
                new MailMessage(from: "2100005423@nttu.edu.vn",
                                to: email,
                                subject,
                                message
                                ));
        }
    }
}
