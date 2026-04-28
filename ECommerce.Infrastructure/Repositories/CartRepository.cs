using ECommerce.Domain.Entities;
using ECommerce.Domain.Interfaces;
using ECommerce.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.Infrastructure.Repositories
{
    public class CartRepository : ICartRepository
    {
        private readonly AppDbContext _context;

        public CartRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task AddToCart(CartItem item)
        {
            var existingItem = await _context.CartItems
                .FirstOrDefaultAsync(x => x.Username == item.Username && x.ProductId == item.ProductId);

            if (existingItem != null)
            {
                existingItem.Quantity += item.Quantity;
            }
            else
            {
                await _context.CartItems.AddAsync(item);
            }

            await _context.SaveChangesAsync();
        }

        public async Task<IEnumerable<CartItem>> GetCart(string username)
        {
            return await _context.CartItems
                .Include(x => x.Product)
                .Where(x => x.Username == username)
                .ToListAsync();
        }

        public async Task UpdateQuantity(int id, int quantity)
        {
            var item = await _context.CartItems.FindAsync(id);

            if (item != null)
            {
                item.Quantity = quantity;
                await _context.SaveChangesAsync();
            }
        }

        public async Task RemoveFromCart(int id)
        {
            var item = await _context.CartItems.FindAsync(id);

            if (item != null)
            {
                _context.CartItems.Remove(item);
                await _context.SaveChangesAsync();
            }
        }
    }
}
