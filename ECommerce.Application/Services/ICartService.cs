using ECommerce.Application.DTOs;
using ECommerce.Domain.Entities;

namespace ECommerce.Application.Services
{
    public interface ICartService
    {
        Task AddToCart(string username, CartDto dto);
        Task<IEnumerable<CartItem>> GetCart(string username);
        Task RemoveFromCart(int id);
    }
}
