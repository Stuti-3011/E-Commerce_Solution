using ECommerce.Domain.Entities;

namespace ECommerce.Domain.Interfaces
{
    public interface ICartRepository
    {
        Task AddToCart(CartItem item);
        Task<IEnumerable<CartItem>> GetCart(string username);
        Task UpdateQuantity(int id, int quantity);
        Task RemoveFromCart(int id);
        Task ClearCart(string username);
        Task MergeCartAsync(string sourceUsername, string destinationUsername);
    }
}
