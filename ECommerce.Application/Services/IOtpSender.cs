namespace ECommerce.Application.Services
{
    public interface IOtpSender
    {
        Task SendAsync(string phoneOrEmail, string otp);
    }
}
