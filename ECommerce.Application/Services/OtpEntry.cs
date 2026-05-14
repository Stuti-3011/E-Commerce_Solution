namespace ECommerce.Application.Services
{
    public class OtpEntry
    {
        public string PhoneOrEmail { get; set; } = string.Empty;
        public string Otp { get; set; } = string.Empty;
        public DateTime ExpiryTime { get; set; }
        public DateTime LastSentAt { get; set; }
        public int AttemptCount { get; set; }
        public int ResendCount { get; set; }
    }
}
