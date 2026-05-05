using ECommerce.Application.DTOs;
using ECommerce.Application.Services;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;

namespace ECommerce.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CartController : ControllerBase
    {
        private readonly ICartService _service;

        public CartController(ICartService service)
        {
            _service = service;
        }

        [HttpPost]
        public async Task<IActionResult> AddToCart(CartDto dto)
        {
            var username = User.Identity?.Name ?? "guest";

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
            var username = User.Identity?.Name ?? "guest";

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
    }
}
