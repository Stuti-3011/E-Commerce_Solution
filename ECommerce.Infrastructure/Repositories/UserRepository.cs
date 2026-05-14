using ECommerce.Domain.Entities;
using ECommerce.Domain.Interfaces;
using ECommerce.Infrastructure.Data;
using System;
using System.Collections.Generic;
using System.Text;

namespace ECommerce.Infrastructure.Repositories
{
    public class UserRepository : IUserRepository
    {
        private readonly AppDbContext _context;

        public UserRepository(AppDbContext context)
        {
            _context = context;
        }

        public User GetByUsername(string username)
        {        
            return _context.Users.FirstOrDefault(x => x.Username == username);
        }

        public User? GetByIdentity(string phoneOrEmail)
        {
            return _context.Users.FirstOrDefault(x =>
                x.Username == phoneOrEmail ||
                x.Email == phoneOrEmail ||
                x.Mobile == phoneOrEmail);
        }

        public void Add(User user)
        {
            _context.Users.Add(user);
        }

        public void Save()
        {
            _context.SaveChanges();
        }
    }
}
