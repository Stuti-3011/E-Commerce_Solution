using ECommerce.Application.Services;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Net;
using System.Net.Mail;

namespace ECommerce.Infrastructure.Services
{
    public class DevelopmentOtpSender : IOtpSender
    {
        private readonly IHostEnvironment _environment;
        private readonly ILogger<DevelopmentOtpSender> _logger;
        private readonly SmtpOptions _smtpOptions;

        public DevelopmentOtpSender(
            IHostEnvironment environment,
            ILogger<DevelopmentOtpSender> logger,
            IOptions<SmtpOptions> smtpOptions)
        {
            _environment = environment;
            _logger = logger;
            _smtpOptions = smtpOptions.Value;
        }

        public async Task SendAsync(string phoneOrEmail, string otp)
        {
            if (phoneOrEmail.Contains('@', StringComparison.Ordinal))
            {
                await SendEmailOtp(phoneOrEmail, otp);
                return;
            }

            if (_environment.IsDevelopment())
            {
                _logger.LogInformation("Phone OTP for {PhoneOrEmail}: {Otp}", phoneOrEmail, otp);
            }
            else
            {
                _logger.LogInformation("Phone OTP generated for {PhoneOrEmail}", phoneOrEmail);
            }
        }

        private async Task SendEmailOtp(string email, string otp)
        {
            if (!IsSmtpConfigured())
            {
                if (_environment.IsDevelopment())
                {
                    _logger.LogWarning("SMTP is not configured. Logging email OTP for development.");
                    _logger.LogInformation("Email OTP for {Email}: {Otp}", email, otp);
                    return;
                }

                throw new InvalidOperationException("SMTP is not configured for email OTP delivery.");
            }

            using var message = new MailMessage
            {
                From = new MailAddress(_smtpOptions.FromEmail, _smtpOptions.FromName),
                Subject = "Your OTP for E-Commerce",
                Body = $"Your OTP is {otp}. It will expire in 5 minutes.",
                IsBodyHtml = false
            };

            message.To.Add(email);

            using var client = new SmtpClient(_smtpOptions.Host, _smtpOptions.Port)
            {
                EnableSsl = _smtpOptions.EnableSsl,
                Credentials = new NetworkCredential(_smtpOptions.Username, _smtpOptions.Password)
            };

            await client.SendMailAsync(message);
        }

        private bool IsSmtpConfigured()
        {
            return !string.IsNullOrWhiteSpace(_smtpOptions.Host)
                && _smtpOptions.Port > 0
                && !string.IsNullOrWhiteSpace(_smtpOptions.Username)
                && !string.IsNullOrWhiteSpace(_smtpOptions.Password)
                && !string.IsNullOrWhiteSpace(_smtpOptions.FromEmail);
        }
    }
}
