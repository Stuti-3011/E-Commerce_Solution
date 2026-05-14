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
        :{
            var existingItem = await _context.CartItems
                .FirstOrDefaultAsync(x => x.Username == item.Username && x.ProductId == item.ProductId && x.SelectedSize == item.SelectedSize);

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
                .ThenInclude(product => product.ProductImages)
                .Include(x => x.Product)
                .ThenInclude(product => product.Sizes)
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

        public async Task ClearCart(string username)
        {
            var items = await _context.CartItems
                .Where(item => item.Username == username)
                .ToListAsync();

            if (items.Count == 0)
            {
                return;
            }

            _context.CartItems.RemoveRange(items);
            await _context.SaveChangesAsync();
        }

        public async Task MergeCartAsync(string sourceUsername, string destinationUsername)
        {
            if (string.IsNullOrWhiteSpace(sourceUsername) ||
                string.IsNullOrWhiteSpace(destinationUsername) ||
                string.Equals(sourceUsername, destinationUsername, StringComparison.Ordinal))
            {
                return;
            }

            var sourceItems = await _context.CartItems
                .Where(item => item.Username == sourceUsername)
                .ToListAsync();

            if (sourceItems.Count == 0)
            {
                return;
            }

            foreach (var sourceItem in sourceItems)
            {
                var destinationItem = await _context.CartItems.FirstOrDefaultAsync(item =>
                    item.Username == destinationUsername &&
                    item.ProductId == sourceItem.ProductId &&
                    item.SelectedSize == sourceItem.SelectedSize);

                if (destinationItem != null)
                {
                    destinationItem.Quantity += sourceItem.Quantity;
                    _context.CartItems.Remove(sourceItem);
                }
                else
                {
                    sourceItem.Username = destinationUsername;
                }
            }

            await _context.SaveChangesAsync();
        }
    }
}
