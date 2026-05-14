using ECommerce.Application.DTOs;
using ECommerce.Application.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System.ComponentModel.DataAnnotations;

namespace ECommerce.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CartController : ControllerBase
    {
        private readonly ICartService _service;
        private readonly ILogger<CartController> _logger;

        public CartController(ICartService service, ILogger<CartController> logger)
        {
            _service = service;
            _logger = logger;
        }

        [HttpPost]
        public async Task<IActionResult> AddToCart(CartDto dto)
        {
            var username = ResolveCartOwner();

            try
            {
                await _service.AddToCart(username, dto);
                return Ok("Item added to cart");
            }
            catch (ValidationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetCart()
        {
            var username = ResolveCartOwner();

            return Ok(await _service.GetCart(username));
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateQuantity(int id, UpdateCartQuantityDto dto)
        {
            await _service.UpdateQuantity(id, dto.Quantity);
            return Ok("Cart quantity updated");
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> RemoveFromCart(int id)
        {
            await _service.RemoveFromCart(id);
            return Ok("Item removed from cart");
        }

        private string ResolveCartOwner()
        {
            var username = User.Identity?.Name;

            if (!string.IsNullOrWhiteSpace(username))
            {
                return username;
            }

            if (Request.Headers.TryGetValue("X-Cart-Session-Id", out var sessionId) &&
                !string.IsNullOrWhiteSpace(sessionId))
            {
                var guestOwner = $"guest:{sessionId.ToString().Trim()}";
                _logger.LogInformation("Using guest cart owner {GuestOwner}", guestOwner);
                return guestOwner;
            }

            _logger.LogWarning("No authenticated user or guest cart session id found. Falling back to shared guest cart.");
            return "guest";
        }
    }
}
