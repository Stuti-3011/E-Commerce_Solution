using ECommerce.Domain.Entities;
using ECommerce.Domain.Interfaces;
using ECommerce.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.Infrastructure.Repositories
{
    public class WishlistRepository : IWishlistRepository
    {
        private readonly AppDbContext _context;

        public WishlistRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task AddToWishlist(WishlistItem item)
        {
            var existingItem = await _context.WishlistItems
                .FirstOrDefaultAsync(x => x.Username == item.Username && x.ProductId == item.ProductId);

            if (existingItem == null)
            {
                await _context.WishlistItems.AddAsync(item);
                await _context.SaveChangesAsync();
            }
        }

        public async Task<IEnumerable<WishlistItem>> GetWishlist(string username)
        {
            return await _context.WishlistItems
                .Include(x => x.Product)
                .Where(x => x.Username == username)
                .ToListAsync();
        }

        public async Task RemoveFromWishlist(int id)
        {
            var item = await _context.WishlistItems.FindAsync(id);

            if (item != null)
            {
                _context.WishlistItems.Remove(item);
                await _context.SaveChangesAsync();
            }
        }
    }
}
