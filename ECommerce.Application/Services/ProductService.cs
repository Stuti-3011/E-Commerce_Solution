using ECommerce.Application.DTOs;
using ECommerce.Domain.Entities;
using ECommerce.Domain.Interfaces;
using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;
using System.Linq;

namespace ECommerce.Application.Services
{
    public class ProductService : IProductService
    {
        private const int MaxImages = 6;
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
            {
                throw new Exception("Product not found");
            }

            return product;
        }

        public async Task CreateProduct(ProductDto dto)
        {
            ValidateProduct(dto, requireImages: true);

            var sizes = NormalizeSizes(dto.Sizes);
            var savedImages = await SaveImages(dto.Images);
            var primaryIndex = NormalizePrimaryIndex(dto.PrimaryImageIndex, savedImages.Count);

            var product = new Product
            {
                Name = dto.Name.Trim(),
                Price = dto.Price,
                Description = dto.Description?.Trim(),
                Stock = dto.Stock,
                ImageUrl = savedImages[primaryIndex],
                ProductImages = savedImages.Select((path, index) => new ProductImage
                {
                    ImageUrl = path,
                    DisplayOrder = index,
                    IsPrimary = index == primaryIndex
                }).ToList(),
                Sizes = sizes.Select((size, index) => new ProductSize
                {
                    Size = size,
                    DisplayOrder = index
                }).ToList()
            };

            await _repo.AddAsync(product);
        }

        public async Task UpdateProduct(int id, ProductDto dto)
        {
            var product = await _repo.GetByIdAsync(id);

            if (product == null)
            {
                throw new Exception("Product not found");
            }

            ValidateProduct(dto, requireImages: product.ProductImages.Count == 0 && string.IsNullOrWhiteSpace(product.ImageUrl));

            product.Name = dto.Name.Trim();
            product.Price = dto.Price;
            product.Description = dto.Description?.Trim();
            product.Stock = dto.Stock;

            var sizes = NormalizeSizes(dto.Sizes);
            product.Sizes.Clear();
            foreach (var size in sizes.Select((value, index) => new ProductSize
            {
                Size = value,
                DisplayOrder = index
            }))
            {
                product.Sizes.Add(size);
            }

            if (dto.Images.Count > 0)
            {
                DeleteProductImages(product);

                var savedImages = await SaveImages(dto.Images);
                var primaryIndex = NormalizePrimaryIndex(dto.PrimaryImageIndex, savedImages.Count);

                product.ImageUrl = savedImages[primaryIndex];
                product.ProductImages.Clear();

                foreach (var image in savedImages.Select((path, index) => new ProductImage
                {
                    ImageUrl = path,
                    DisplayOrder = index,
                    IsPrimary = index == primaryIndex
                }))
                {
                    product.ProductImages.Add(image);
                }
            }

            await _repo.UpdateAsync(product);
        }

        public async Task DeleteProduct(int id)
        {
            var product = await _repo.GetByIdAsync(id);

            if (product == null)
            {
                throw new Exception("Product not found");
            }

            DeleteProductImages(product);
            await _repo.DeleteAsync(id);
        }

        private static void ValidateProduct(ProductDto dto, bool requireImages)
        {
            if (string.IsNullOrWhiteSpace(dto.Name))
            {
                throw new ValidationException("Product name is required.");
            }

            if (dto.Price <= 0)
            {
                throw new ValidationException("Product price must be greater than zero.");
            }

            if (dto.Stock < 0)
            {
                throw new ValidationException("Product stock cannot be negative.");
            }

            if (dto.Images.Count > MaxImages)
            {
                throw new ValidationException($"You can upload up to {MaxImages} images per product.");
            }

            if (requireImages && dto.Images.Count == 0)
            {
                throw new ValidationException("At least one product image is required.");
            }
        }

        private static List<string> NormalizeSizes(IEnumerable<string> sizes)
        {
            return sizes
                .Select(size => size?.Trim())
                .Where(size => !string.IsNullOrWhiteSpace(size))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Cast<string>()
                .ToList();
        }

        private static int NormalizePrimaryIndex(int primaryImageIndex, int imageCount)
        {
            if (imageCount == 0)
            {
                return 0;
            }

            return primaryImageIndex >= 0 && primaryImageIndex < imageCount ? primaryImageIndex : 0;
        }

        private static string GetImageDirectory()
        {
            return Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "images");
        }

        private async Task<List<string>> SaveImages(IEnumerable<IFormFile> images)
        {
            var imageDirectory = GetImageDirectory();

            if (!Directory.Exists(imageDirectory))
            {
                Directory.CreateDirectory(imageDirectory);
            }

            var savedImages = new List<string>();

            foreach (var image in images)
            {
                var fileName = $"{Guid.NewGuid()}{Path.GetExtension(image.FileName)}";
                var fullPath = Path.Combine(imageDirectory, fileName);

                using var stream = new FileStream(fullPath, FileMode.Create);
                await image.CopyToAsync(stream);

                savedImages.Add($"/images/{fileName}");
            }

            return savedImages;
        }

        private static void DeleteProductImages(Product product)
        {
            var imagePaths = product.ProductImages.Select(image => image.ImageUrl).ToList();

            if (imagePaths.Count == 0 && !string.IsNullOrWhiteSpace(product.ImageUrl))
            {
                imagePaths.Add(product.ImageUrl);
            }

            foreach (var imagePath in imagePaths.Distinct(StringComparer.OrdinalIgnoreCase))
            {
                if (string.IsNullOrWhiteSpace(imagePath))
                {
                    continue;
                }

                var relativePath = imagePath.TrimStart('/').Replace('/', Path.DirectorySeparatorChar);
                var fullPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", relativePath);

                if (File.Exists(fullPath))
                {
                    File.Delete(fullPath);
                }
            }
        }
    }
}
