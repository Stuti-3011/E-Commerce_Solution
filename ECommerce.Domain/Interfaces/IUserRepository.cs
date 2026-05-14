using ECommerce.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace ECommerce.Domain.Interfaces
{
    public interface IUserRepository
    {
        User GetByUsername(string username);
        User? GetByIdentity(string phoneOrEmail);
        void Add(User user);
        void Save();
    }
}
