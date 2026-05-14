namespace ECommerce.Application.DTOs
{
    public class VerifyOtpRequestDto
    {
        public string PhoneOrEmail { get; set; } = string.Empty;
        public string Otp { get; set; } = string.Empty;
        public string? GuestCartSessionId { get; set; }
    }
}
