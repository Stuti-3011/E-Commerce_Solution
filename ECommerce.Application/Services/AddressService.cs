using ECommerce.Application.DTOs;
using ECommerce.Domain.Entities;
using ECommerce.Domain.Interfaces;

namespace ECommerce.Application.Services
{
    public class AddressService : IAddressService
    {
        private readonly IAddressRepository _addressRepo;
        private readonly IUserRepository _userRepo;

        public AddressService(IAddressRepository addressRepo, IUserRepository userRepo)
        {
            _addressRepo = addressRepo;
            _userRepo = userRepo;
        }

        public async Task<IEnumerable<Address>> GetAddresses(string username)
        {
            var user = GetUser(username);
            return await _addressRepo.GetByUserId(user.Id);
        }

        public async Task<Address> AddAddress(string username, AddressDto dto)
        {
            var user = GetUser(username);
            var address = MapAddress(user.Id, dto);
            return await _addressRepo.Add(address);
        }

        public async Task<Address> UpdateAddress(string username, int id, AddressDto dto)
        {
            var user = GetUser(username);
            var address = MapAddress(user.Id, dto);
            return await _addressRepo.Update(id, address);
        }

        public async Task SetDefaultAddress(string username, int id)
        {
            var user = GetUser(username);
            await _addressRepo.SetDefault(user.Id, id);
        }

        private User GetUser(string username)
        {
            var user = _userRepo.GetByUsername(username);

            if (user == null)
            {
                throw new Exception("User not found");
            }

            return user;
        }

        private static Address MapAddress(int userId, AddressDto dto)
        {
            return new Address
            {
                UserId = userId,
                RecipientName = dto.RecipientName,
                Phone = dto.Phone,
                AddressLine = dto.AddressLine,
                City = dto.City,
                Pincode = dto.Pincode,
                IsDefault = dto.IsDefault
            };
        }
    }
}
