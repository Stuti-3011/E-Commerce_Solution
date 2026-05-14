using ECommerce.Application.DTOs;
using ECommerce.Domain.Entities;

namespace ECommerce.Application.Services
{
    public interface IPaymentService
    {
        Task<CreatePaymentOrderResponseDto> CreateOrderAsync(string username);
        Task<Order> CreateLocalOrderAsync(string username);
        Task<RazorpayOrderResultDto> CreateRazorpayOrderAsync(Order order);
        bool VerifyPaymentSignature(string orderId, string paymentId, string signature);
        Task<PaymentResultDto> FinalizePaymentAsync(string username, VerifyPaymentDto dto);  
    }
}
