namespace ECommerce.Application.DTOs
{
    public class CreatePaymentOrderResponseDto
    {
        public string KeyId { get; set; } = string.Empty;
        public string RazorpayOrderId { get; set; } = string.Empty;
        public long Amount { get; set; }
        public int LocalOrderId { get; set; }
    }
}
  