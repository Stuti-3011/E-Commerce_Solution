using ECommerce.Application.DTOs;
using ECommerce.Domain.Entities;

namespace ECommerce.Application.Services
{
    public interface IAddressService
    {
        Task<IEnumerable<Address>> GetAddresses(string username);
        Task<Address> AddAddress(string username, AddressDto dto);
        Task<Address> UpdateAddress(string username, int id, AddressDto dto);
        Task SetDefaultAddress(string username, int id);
    }
}
