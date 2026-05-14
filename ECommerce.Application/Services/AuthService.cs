using ECommerce.Application.DTOs;
using ECommerce.Domain.Entities;
using ECommerce.Domain.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Net.Mail;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

namespace ECommerce.Application.Services
{
    public class AuthService : IAuthService
    {
        private const int OtpLength = 6;
        private const int OtpExpiryMinutes = 5;
        private const int ResendCooldownSeconds = 60;
        private const int MaxInvalidAttempts = 5;
        private const int MaxResendAttempts = 3;

        private readonly IUserRepository _repo;
        private readonly ICartRepository _cartRepository;
        private readonly IOtpStore _otpStore;
        private readonly IOtpSender _otpSender;
        private readonly IConfiguration _config;
        private readonly ILogger<AuthService> _logger;

        public AuthService(
            IUserRepository repo,
            ICartRepository cartRepository,
            IOtpStore otpStore,
            IOtpSender otpSender,
            IConfiguration config,
            ILogger<AuthService> logger)
        {
            _repo = repo;
            _cartRepository = cartRepository;
            _otpStore = otpStore;
            _otpSender = otpSender;
            _config = config;
            _logger = logger;
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

        public User? ValidateUser(LoginDto dto)
        {
            var user = _repo.GetByUsername(dto.Username);

            if (user == null || !BCrypt.Net.BCrypt.Verify(dto.Password, user.PasswordHash))
                return null;

            return user;
        }

        public async Task SendOtpAsync(SendOtpRequestDto dto)
        {
            var normalizedIdentity = NormalizePhoneOrEmail(dto.PhoneOrEmail);

            if (!IsValidPhoneOrEmail(normalizedIdentity))
                throw new Exception("Please enter a valid phone number or email address.");

            var existingEntry = _otpStore.Get(normalizedIdentity);

            if (existingEntry != null)
            {
                if (existingEntry.ExpiryTime > DateTime.UtcNow &&
                    existingEntry.LastSentAt.AddSeconds(ResendCooldownSeconds) > DateTime.UtcNow)
                {
                    throw new Exception("Please wait before requesting another OTP.");
                }

                if (existingEntry.ExpiryTime > DateTime.UtcNow &&
                    existingEntry.ResendCount >= MaxResendAttempts)
                {
                    throw new Exception("OTP resend limit reached. Please try again later.");
                }
            }

            var otp = GenerateOtp();

            var entry = new OtpEntry
            {
                PhoneOrEmail = normalizedIdentity,
                Otp = otp,
                ExpiryTime = DateTime.UtcNow.AddMinutes(OtpExpiryMinutes),
                LastSentAt = DateTime.UtcNow,
                AttemptCount = 0,
                ResendCount = existingEntry == null ? 0 : existingEntry.ResendCount + 1
            };

            _otpStore.Save(entry);
            await _otpSender.SendAsync(normalizedIdentity, otp);
        }

        public async Task<AuthResponseDto> VerifyOtpAsync(VerifyOtpRequestDto dto)
        {
            var normalizedIdentity = NormalizePhoneOrEmail(dto.PhoneOrEmail);

            if (!IsValidPhoneOrEmail(normalizedIdentity))
                throw new Exception("Please enter a valid phone number or email address.");

            var entry = _otpStore.Get(normalizedIdentity);

            if (entry == null)
                throw new Exception("OTP not found. Please request a new OTP.");

            if (entry.ExpiryTime <= DateTime.UtcNow)
            {
                _otpStore.Remove(normalizedIdentity);
                throw new Exception("OTP has expired. Please request a new OTP.");
            }

            if (!string.Equals(entry.Otp, dto.Otp?.Trim(), StringComparison.Ordinal))
            {
                entry.AttemptCount++;

                if (entry.AttemptCount >= MaxInvalidAttempts)
                {
                    _otpStore.Remove(normalizedIdentity);
                    throw new Exception("Too many invalid attempts. Please request a new OTP.");
                }

                _otpStore.Save(entry);
                throw new Exception("Invalid OTP.");
            }

            var user = _repo.GetByIdentity(normalizedIdentity);

            if (user == null)
            {
                user = new User
                {
                    Username = normalizedIdentity,
                    Email = IsEmail(normalizedIdentity) ? normalizedIdentity : null,
                    Mobile = IsPhone(normalizedIdentity) ? normalizedIdentity : null,
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword(CreateInternalPassword()),
                    Role = "User",
                    IsVerified = true,
                    CreatedAt = DateTime.UtcNow
                };

                _repo.Add(user);
                _repo.Save();
            }
            else if (!user.IsVerified)
            {
                user.IsVerified = true;
                _repo.Save();
            }

            var guestCartOwner = BuildGuestCartOwner(dto.GuestCartSessionId);

            if (!string.IsNullOrWhiteSpace(guestCartOwner))
            {
                _logger.LogInformation("Merging guest cart {GuestCartOwner} into user cart {Username}", guestCartOwner, user.Username);
                await _cartRepository.MergeCartAsync(guestCartOwner, user.Username);
            }

            _otpStore.Remove(normalizedIdentity);

            return new AuthResponseDto
            {
                Token = GenerateJwtToken(user),
                Role = user.Role,
                Username = user.Username
            };
        }

        public string GenerateJwtToken(User user)
        {
            var key = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(_config["Jwt:Key"]!)
            );

            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var claims = new[]
            {
                new Claim(ClaimTypes.Name, user.Username),
                new Claim(ClaimTypes.Role, user.Role)
            };

            var token = new JwtSecurityToken(
                claims: claims,
                expires: DateTime.UtcNow.AddHours(2),
                signingCredentials: creds
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        private static string GenerateOtp()
        {
            var value = RandomNumberGenerator.GetInt32(0, 1_000_000);
            return value.ToString($"D{OtpLength}");
        }

        private static string CreateInternalPassword()
        {
            return Convert.ToHexString(RandomNumberGenerator.GetBytes(32));
        }

        private static string NormalizePhoneOrEmail(string value)
        {
            var input = value?.Trim() ?? string.Empty;

            if (string.IsNullOrWhiteSpace(input))
            {
                return string.Empty;
            }

            return input.Contains('@', StringComparison.Ordinal)
                ? input.ToLowerInvariant()
                : Regex.Replace(input, "[^0-9]", string.Empty);
        }

        private static bool IsValidPhoneOrEmail(string value)
        {
            return IsEmail(value) || IsPhone(value);
        }

        private static bool IsEmail(string value)
        {
            try
            {
                var address = new MailAddress(value);
                return string.Equals(address.Address, value, StringComparison.OrdinalIgnoreCase);
            }
            catch
            {
                return false;
            }
        }

        private static bool IsPhone(string value)
        {
            return Regex.IsMatch(value, "^[0-9]{10,15}$");
        }

        private static string? BuildGuestCartOwner(string? guestCartSessionId)
        {
            if (string.IsNullOrWhiteSpace(guestCartSessionId))
            {
                return null;
            }

            return $"guest:{guestCartSessionId.Trim()}";
        }
    }
}
