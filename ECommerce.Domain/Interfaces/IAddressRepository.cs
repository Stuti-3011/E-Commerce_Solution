using ECommerce.Domain.Entities;

namespace ECommerce.Domain.Interfaces
{
    public interface IAddressRepository
    {
        Task<IEnumerable<Address>> GetByUserId(int userId);
        Task<Address> Add(Address address);
        Task<Address> Update(int id, Address address);
        Task SetDefault(int userId, int id);
    }
}
