using ECommerce.Domain.Entities;

namespace ECommerce.Application.Services
{
    public interface IWishlistService
    {
        Task AddToWishlist(string username, int productId);
        Task<IEnumerable<WishlistItem>> GetWishlist(string username);
        Task RemoveFromWishlist(int id);
    }
}
