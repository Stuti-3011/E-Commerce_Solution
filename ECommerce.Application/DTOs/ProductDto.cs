using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Text;

namespace ECommerce.Application.DTOs
{
    public class ProductDto
    {
        public string Name { get; set; }
        public decimal Price { get; set; }
        public string Description { get; set; }
        public int Stock { get; set; }
        public List<IFormFile> Images { get; set; } = new List<IFormFile>();
        public int PrimaryImageIndex { get; set; }
        public List<string> Sizes { get; set; } = new List<string>();
    }
}
