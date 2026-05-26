using ECommerce.Application.DTOs;
using ECommerce.Domain.Entities;
using ECommerce.Domain.Interfaces;
using System.ComponentModel.DataAnnotations;
using System.Linq;

namespace ECommerce.Application.Services
{
    public class CartService : ICartService
    {
        private readonly ICartRepository _repo;
        private readonly IProductRepository _productRepository;

        public CartService(ICartRepository repo, IProductRepository productRepository)
        {
            _repo = repo;
            _productRepository = productRepository;
        }

        public async Task AddToCart(string username, CartDto dto)
        {
            var product = await _productRepository.GetByIdAsync(dto.ProductId);

            if (product == null)
            {
                throw new ValidationException("Product not found.");
            }

            var availableSizes = product.Sizes
                .Select(size => size.Size?.Trim())
                .Where(size => !string.IsNullOrWhiteSpace(size))
                .Cast<string>()
                .ToList();

            var selectedSize = dto.SelectedSize?.Trim();

            if (availableSizes.Count > 0)
            {
                if (string.IsNullOrWhiteSpace(selectedSize))
                {
                    throw new ValidationException("Please select a size before adding this item to cart.");
                }

                if (!availableSizes.Contains(selectedSize, StringComparer.OrdinalIgnoreCase))
                {
                    throw new ValidationException("The selected size is not available for this product.");
                }

                var selectedProductSize = product.Sizes.FirstOrDefault(size =>
                    string.Equals(size.Size, selectedSize, StringComparison.OrdinalIgnoreCase));

                if (selectedProductSize == null || selectedProductSize.StockQuantity <= 0)
                {
                    throw new ValidationException("Selected size is out of stock.");
                }
            }

            var existingItems = await _repo.GetCart(username);
            var existingQuantity = existingItems
                .Where(item => item.ProductId == dto.ProductId &&
                               string.Equals(item.SelectedSize, selectedSize, StringComparison.OrdinalIgnoreCase))
                .Sum(item => item.Quantity);

            var requestedQuantity = existingQuantity + dto.Quantity;
            ValidateRequestedStock(product, requestedQuantity, selectedSize);

            var item = new CartItem
            {
                Username = username,
                ProductId = dto.ProductId,
                Quantity = dto.Quantity,
                SelectedSize = string.IsNullOrWhiteSpace(selectedSize) ? null : selectedSize
            };

            await _repo.AddToCart(item);
        }

        public async Task<IEnumerable<CartItem>> GetCart(string username)
        {
            return await _repo.GetCart(username);
        }

        public async Task UpdateQuantity(int id, int quantity)
        {
            var item = await _repo.GetCartItemByIdAsync(id);

            if (item == null)
            {
                throw new ValidationException("Cart item not found.");
            }

            ValidateRequestedStock(item.Product, quantity, item.SelectedSize);
            await _repo.UpdateQuantity(id, quantity);
        }

        public async Task RemoveFromCart(int id)
        {
            await _repo.RemoveFromCart(id);
        }

        private static void ValidateRequestedStock(Product product, int requestedQuantity, string? selectedSize)
        {
            if (requestedQuantity <= 0)
            {
                throw new ValidationException("Quantity must be at least 1.");
            }

            if (product.Sizes.Count > 0)
            {
                var size = product.Sizes.FirstOrDefault(productSize =>
                    string.Equals(productSize.Size, selectedSize, StringComparison.OrdinalIgnoreCase));

                if (size == null || size.StockQuantity <= 0 || requestedQuantity > size.StockQuantity)
                {
                    throw new ValidationException("Selected size is out of stock.");
                }

                return;
            }

            if (product.Stock <= 0 || requestedQuantity > product.Stock)
            {
                throw new ValidationException("Selected product is out of stock.");
            }
        }
    }
}
