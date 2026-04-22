using ECommerce.Application.DTOs;
using ECommerce.Domain.Entities;
using ECommerce.Domain.Interfaces;
using System;
using System.Collections.Generic;
using System.Text;

namespace ECommerce.Application.Services
{
    public class ProductService : IProductService
    {
        private readonly IProductRepository _repo;

        public ProductService(IProductRepository repo)
        {
            _repo = repo;
        }

        public async Task<IEnumerable<Product>> GetAllProducts()
        {
            return await _repo.GetAllAsync();
        }

        public async Task<Product> GetProductById(int id)
        {
            var product = await _repo.GetByIdAsync(id);

            if (product == null)
                throw new Exception("Product not found");

            return product;
        }

        public async Task CreateProduct(ProductDto dto)
        {
            var product = new Product
            {
                Name = dto.Name,
                Price = dto.Price,
                Description = dto.Description,
                Stock = dto.Stock
            };

            await _repo.AddAsync(product);
        }

        public async Task UpdateProduct(int id, ProductDto dto)
        {
            var product = await _repo.GetByIdAsync(id);

            if (product == null)
                throw new Exception("Product not found");

            product.Name = dto.Name;
            product.Price = dto.Price;
            product.Description = dto.Description;
            product.Stock = dto.Stock;

            await _repo.UpdateAsync(product);
        }

        public async Task DeleteProduct(int id)
        {
            var product = await _repo.GetByIdAsync(id);

            if (product == null)
                throw new Exception("Product not found");

            await _repo.DeleteAsync(id);
        }
    }
}
