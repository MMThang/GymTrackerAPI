using GymTracker.Exceptions;
using GymTracker.Interfaces;
using MailKit.Net.Smtp;
using MimeKit;

namespace GymTracker.Services
{
    public class EmailService : IEmailService
    {
        private readonly IConfiguration _configuration;

        public EmailService(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public async Task SendVerificationCodeAsync(
            string email,
            string code)
        {
            var smtpServer = GetRequiredConfiguration("EmailService:SmtpServer");
            var smtpPort = GetRequiredConfiguration("EmailService:SmtpPort");
            var username = GetRequiredConfiguration("EmailService:Username");
            var password = GetRequiredConfiguration("EmailService:Password");

            var message = new MimeMessage();

            message.From.Add(new MailboxAddress(
                "GymTracker",
                username
            ));

            message.To.Add(new MailboxAddress(
                "",
                email
            ));

            message.Subject = "GymTracker Email Verification";

            message.Body = new TextPart("plain")
            {
                Text = $"""
                    Welcome to GymTracker!

                    Your email verification code is:

                    {code}

                    This code will expire in 5 minutes.

                    If you did not create a GymTracker account,
                    you can ignore this email.
                    """
            };

            using var smtp = new SmtpClient();

            //Production
            await smtp.ConnectAsync(
                smtpServer,
                int.Parse(smtpPort),
                MailKit.Security.SecureSocketOptions.StartTls
            );

            await smtp.AuthenticateAsync(
                username,
                password
            );
            //Production-END

            //Development
            // var useTls = bool.Parse(
            //     _configuration["EmailService:UseTls"]!
            // );

            // await smtp.ConnectAsync(
            //     _configuration["EmailService:SmtpServerTest"],
            //     int.Parse(_configuration["EmailService:SmtpPortTest"]!),
            //     useTls
            //         ? MailKit.Security.SecureSocketOptions.StartTls
            //         : MailKit.Security.SecureSocketOptions.None
            // );

            // if (useTls)
            // {
            //     await smtp.AuthenticateAsync(
            //         _configuration["EmailService:Username"],
            //         _configuration["EmailService:Password"]
            //     );
            // }
            //Development-END

            await smtp.SendAsync(message);

            await smtp.DisconnectAsync(true);
        }

        private string GetRequiredConfiguration(string key)
        {
            var value = _configuration[key];

            if (value == null)
            {
                throw new EmailConfigurationException(key);
            }

            return value;
        }
    }
}