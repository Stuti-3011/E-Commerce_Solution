using ECommerce.Domain.Entities;
using ECommerce.Domain.Interfaces;

namespace ECommerce.Application.Services
{
    public class WishlistService : IWishlistService
    {
        private readonly IWishlistRepository _repo;

        public WishlistService(IWishlistRepository repo)
        {
            _repo = repo;
        }

        public async Task AddToWishlist(string username, int productId)
        {
            var item = new WishlistItem
            {
                Username = username,
                ProductId = productId
            };

            await _repo.AddToWishlist(item);
        }

        public async Task<IEnumerable<WishlistItem>> GetWishlist(string username)
        {
            return await _repo.GetWishlist(username);
        }

        public async Task RemoveFromWishlist(int id)
        {
            await _repo.RemoveFromWishlist(id);
        }
    }
}
