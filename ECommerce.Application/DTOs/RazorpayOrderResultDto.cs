namespace ECommerce.Application.DTOs
{
    public class RazorpayOrderResultDto
    {
        public string RazorpayOrderId { get; set; } = string.Empty;
        public long Amount { get; set; }
    }
}
