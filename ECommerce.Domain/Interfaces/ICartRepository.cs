using ECommerce.Domain.Entities;

namespace ECommerce.Domain.Interfaces
{
    public interface ICartRepository
    {
        Task AddToCart(CartItem item);
        Task<IEnumerable<CartItem>> GetCart(string username);
        Task RemoveFromCart(int id);
    }
}
