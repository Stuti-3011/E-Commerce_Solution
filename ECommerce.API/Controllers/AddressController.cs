using ECommerce.Application.DTOs;
using ECommerce.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ECommerce.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class AddressController : ControllerBase
    {
        private readonly IAddressService _service;

        public AddressController(IAddressService service)
        {
            _service = service;
        }

        [HttpGet]
        public async Task<IActionResult> GetAddresses()
        {
            var username = User.Identity?.Name ?? string.Empty;
            return Ok(await _service.GetAddresses(username));
        }

        [HttpPost]
        public async Task<IActionResult> AddAddress(AddressDto dto)
        {
            var username = User.Identity?.Name ?? string.Empty;
            return Ok(await _service.AddAddress(username, dto));
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateAddress(int id, AddressDto dto)
        {
            var username = User.Identity?.Name ?? string.Empty;
            return Ok(await _service.UpdateAddress(username, id, dto));
        }

        [HttpPut("{id}/default")]
        public async Task<IActionResult> SetDefaultAddress(int id)
        {
            var username = User.Identity?.Name ?? string.Empty;
            await _service.SetDefaultAddress(username, id);
            return Ok("Default address updated");
        }
    }
}
