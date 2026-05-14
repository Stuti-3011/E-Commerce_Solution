using ECommerce.Domain.Entities;

namespace ECommerce.Domain.Interfaces
{
    public interface IPaymentRepository
    {
        Task<Order> AddOrderAsync(Order order);
        Task<Order?> GetOrderByIdAsync(int id);
        Task<Order?> GetOrderByRazorpayOrderIdAsync(string razorpayOrderId);
        Task UpdateOrderAsync(Order order);
        Task<Payment?> GetPaymentByRazorpayPaymentIdAsync(string razorpayPaymentId);
        Task AddPaymentAsync(Payment payment);
        Task UpdatePaymentAsync(Payment payment);
    }
}
