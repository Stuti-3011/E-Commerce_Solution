using System;
using System.Collections.Generic;
using System.Text;

namespace ECommerce.Application.DTOs
{
    public class RegisterDto
    {
        public string? Email { get; set; }
        public string? Mobile { get; set; }
        public string Password { get; set; }
    }
}
