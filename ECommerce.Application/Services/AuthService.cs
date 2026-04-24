using ECommerce.Application.DTOs;
using ECommerce.Domain.Entities;
using ECommerce.Domain.Interfaces;
using System;
using System.Collections.Generic;
using System.Text;

namespace ECommerce.Application.Services
{
    public class AuthService:IAuthService
    {
        private readonly IUserRepository _repo;

        public AuthService(IUserRepository repo)
        {
            _repo = repo;
        }

        public string Register(RegisterDto dto)
        {
            if (string.IsNullOrEmpty(dto.Email) && string.IsNullOrEmpty(dto.Mobile))
                throw new Exception("Email or Mobile is required");

            var username = dto.Email ?? dto.Mobile;

            var existingUser = _repo.GetByUsername(username);
            if (existingUser != null)
                throw new Exception("User already exists");

            var user = new User
            {
                Username = username,
                Email = dto.Email,
                Mobile = dto.Mobile,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password),
                Role = "User",
                IsVerified = true,
                CreatedAt = DateTime.UtcNow
            };

            _repo.Add(user);
            _repo.Save();

            return "User Registered Successfully";
        }

        public User ValidateUser(LoginDto dto)
        {
            var user = _repo.GetByUsername(dto.Username);

            if (user == null || !BCrypt.Net.BCrypt.Verify(dto.Password, user.PasswordHash))
                return null;

            return user;
        }
    }
}