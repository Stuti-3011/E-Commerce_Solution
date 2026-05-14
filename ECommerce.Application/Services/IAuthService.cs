using ECommerce.Application.DTOs;
using System;
using System.Collections.Generic;
using System.Text;

namespace ECommerce.Domain.Entities
{
    public interface IAuthService
    {
        string Register(RegisterDto dto);
        User? ValidateUser(LoginDto dto);
        Task SendOtpAsync(SendOtpRequestDto dto);
        Task<AuthResponseDto> VerifyOtpAsync(VerifyOtpRequestDto dto);
        string GenerateJwtToken(User user);
    }
}
