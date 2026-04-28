using ECommerce.Domain.Entities;

namespace ECommerce.Domain.Interfaces
{
    public interface IWishlistRepository
    {
        Task AddToWishlist(WishlistItem item);
        Task<IEnumerable<WishlistItem>> GetWishlist(string username);
        Task RemoveFromWishlist(int id);
    }
}
