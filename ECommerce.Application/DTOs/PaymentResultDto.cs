namespace ECommerce.Application.DTOs
{
    public class PaymentResultDto
    {
        public bool Success { get; set; }
        public string Status { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
    }
}
