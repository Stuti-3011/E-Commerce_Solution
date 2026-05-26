using ECommerce.Domain.Entities;
using ECommerce.Domain.Interfaces;
using ECommerce.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.Data;

namespace ECommerce.Infrastructure.Repositories
{
    public class ProductRepository : IProductRepository
    {
        private readonly AppDbContext _context;

        public ProductRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<Product>> GetAllAsync()
        {
            return await _context.Products
                .Include(product => product.ProductImages.OrderBy(image => image.DisplayOrder))
                .Include(product => product.Sizes.OrderBy(size => size.DisplayOrder))
                .ToListAsync();
        }

        public async Task<Product> GetByIdAsync(int id)
        {
            return await _context.Products
                .Include(product => product.ProductImages.OrderBy(image => image.DisplayOrder))
                .Include(product => product.Sizes.OrderBy(size => size.DisplayOrder))
                .FirstOrDefaultAsync(product => product.Id == id);
        }

        public async Task AddAsync(Product product)
        {
            await _context.Products.AddAsync(product);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateAsync(Product product)
        {
            _context.Products.Update(product);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteAsync(int id)
        {
            var product = await _context.Products.FindAsync(id);
            if (product != null)
            {
                _context.Products.Remove(product);
                await _context.SaveChangesAsync();
            }
        }

        public async Task ValidateCartStockAsync(IEnumerable<CartItem> cartItems)
        {
            var items = cartItems.ToList();

            if (items.Count == 0)
            {
                return;
            }

            var products = await LoadProductsForCartAsync(items);

            foreach (var item in items)
            {
                var product = products.FirstOrDefault(productItem => productItem.Id == item.ProductId);

                if (product == null)
                {
                    throw new ValidationException("Product not found.");
                }

                ValidateItemStock(product, item);
            }
        }

        public async Task ReduceStockForCartAsync(IEnumerable<CartItem> cartItems)
        {
            var items = cartItems.ToList();

            if (items.Count == 0)
            {
                return;
            }

            // Serializable isolation prevents concurrent checkouts from overselling the same size.
            await using var transaction = await _context.Database.BeginTransactionAsync(IsolationLevel.Serializable);
            var products = await LoadProductsForCartAsync(items);

            foreach (var item in items)
            {
                var product = products.FirstOrDefault(productItem => productItem.Id == item.ProductId);

                if (product == null)
                {
                    throw new ValidationException("Product not found.");
                }

                if (product.Sizes.Count > 0)
                {
                    var size = product.Sizes.FirstOrDefault(sizeItem =>
                        string.Equals(sizeItem.Size, item.SelectedSize, StringComparison.OrdinalIgnoreCase));

                    if (size == null || size.StockQuantity < item.Quantity)
                    {
                        throw new ValidationException("Selected size is out of stock.");
                    }

                    size.StockQuantity -= item.Quantity;
                    product.Stock = product.Sizes.Sum(sizeItem => sizeItem.StockQuantity);
                }
                else
                {
                    if (product.Stock < item.Quantity)
                    {
                        throw new ValidationException("Selected product is out of stock.");
                    }

                    product.Stock -= item.Quantity;
                }
            }

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();
        }

        private async Task<List<Product>> LoadProductsForCartAsync(IEnumerable<CartItem> cartItems)
        {
            var productIds = cartItems
                .Select(item => item.ProductId)
                .Distinct()
                .ToList();

            return await _context.Products
                .Include(product => product.Sizes.OrderBy(size => size.DisplayOrder))
                .Where(product => productIds.Contains(product.Id))
                .ToListAsync();
        }

        private static void ValidateItemStock(Product product, CartItem item)
        {
            if (product.Sizes.Count > 0)
            {
                var size = product.Sizes.FirstOrDefault(sizeItem =>
                    string.Equals(sizeItem.Size, item.SelectedSize, StringComparison.OrdinalIgnoreCase));

                if (size == null || size.StockQuantity <= 0 || size.StockQuantity < item.Quantity)
                {
                    throw new ValidationException("Selected size is out of stock.");
                }

                return;
            }

            if (product.Stock <= 0 || product.Stock < item.Quantity)
            {
                throw new ValidationException("Selected product is out of stock.");
            }
        }
    }
}
