using ECommerce.Application.DTOs;
using ECommerce.Domain.Entities;
using ECommerce.Domain.Interfaces;

namespace ECommerce.Application.Services
{
    public class CartService : ICartService
    {
        private readonly ICartRepository _repo;

        public CartService(ICartRepository repo)
        {
            _repo = repo;
        }

        public async Task AddToCart(string username, CartDto dto)
        {
            var item = new CartItem
            {
                Username = username,
                ProductId = dto.ProductId,
                Quantity = dto.Quantity
            };

            await _repo.AddToCart(item);
        }

        public async Task<IEnumerable<CartItem>> GetCart(string username)
        {
            return await _repo.GetCart(username);
        }

        public async Task RemoveFromCart(int id)
        {
            await _repo.RemoveFromCart(id);
        }
    }
}
