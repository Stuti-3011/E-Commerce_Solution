using ECommerce.Application.DTOs;
using ECommerce.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace ECommerce.Application.Services
{
    public interface IProductService
    {
        Task<IEnumerable<Product>> GetAllProducts();
        Task<Product> GetProductById(int id);
        Task CreateProduct(ProductDto dto);
        Task UpdateProduct(int id, ProductDto dto);
        Task DeleteProduct(int id);
    }
}
