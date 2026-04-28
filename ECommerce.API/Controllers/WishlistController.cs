using ECommerce.Application.Services;
using Microsoft.AspNetCore.Mvc;

namespace ECommerce.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class WishlistController : ControllerBase
    {
        private readonly IWishlistService _service;

        public WishlistController(IWishlistService service)
        {
            _service = service;
        }

        [HttpPost]
        public async Task<IActionResult> AddToWishlist([FromBody] int productId)
        {   
            var username = User.Identity?.Name ?? "guest";

            await _service.AddToWishlist(username, productId);
            return Ok("Item added to wishlist");
        }
          
        [HttpGet]
        public async Task<IActionResult> GetWishlist()
        {
            var username = User.Identity?.Name ?? "guest";

            return Ok(await _service.GetWishlist(username));
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> RemoveFromWishlist(int id)
        {
            await _service.RemoveFromWishlist(id);
            return Ok("Item removed from wishlist");
        }
    }
}
