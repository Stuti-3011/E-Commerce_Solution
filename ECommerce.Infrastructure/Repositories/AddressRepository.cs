using ECommerce.Domain.Entities;
using ECommerce.Domain.Interfaces;
using ECommerce.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.Infrastructure.Repositories
{
    public class AddressRepository : IAddressRepository
    {
        private readonly AppDbContext _context;

        public AddressRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<Address>> GetByUserId(int userId)
        {
            return await _context.Addresses
                .Where(x => x.UserId == userId)
                .OrderByDescending(x => x.IsDefault)
                .ThenByDescending(x => x.Id)
                .ToListAsync();
        }

        public async Task<Address> Add(Address address)
        {
            if (address.IsDefault)
            {
                await ClearDefault(address.UserId);
            }

            await _context.Addresses.AddAsync(address);
            await _context.SaveChangesAsync();
            return address;
        }

        public async Task<Address> Update(int id, Address address)
        {
            var existingAddress = await _context.Addresses.FirstOrDefaultAsync(x => x.Id == id && x.UserId == address.UserId);

            if (existingAddress == null)
            {
                throw new Exception("Address not found");
            }

            existingAddress.RecipientName = address.RecipientName;
            existingAddress.Phone = address.Phone;
            existingAddress.AddressLine = address.AddressLine;
            existingAddress.City = address.City;
            existingAddress.Pincode = address.Pincode;

            if (address.IsDefault)
            {
                await ClearDefault(address.UserId);
                existingAddress.IsDefault = true;
            }

            await _context.SaveChangesAsync();
            return existingAddress;
        }

        public async Task SetDefault(int userId, int id)
        {
            await ClearDefault(userId);

            var existingAddress = await _context.Addresses.FirstOrDefaultAsync(x => x.Id == id && x.UserId == userId);

            if (existingAddress == null)
            {
                throw new Exception("Address not found");
            }

            existingAddress.IsDefault = true;
            await _context.SaveChangesAsync();
        }

        private async Task ClearDefault(int userId)
        {
            var addresses = await _context.Addresses.Where(x => x.UserId == userId && x.IsDefault).ToListAsync();

            foreach (var item in addresses)
            {
                item.IsDefault = false;
            }
        }
    }
}
