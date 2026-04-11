using E_Commerce.DataContext;
using E_Commerce.Dtos.Helpers;
using E_Commerce.Dtos.ProductDtos;
using E_Commerce.Entities;
using E_Commerce.UnitOfWork;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;

namespace E_Commerce.Services.ProductService
{
    public class ProdService : IProductService
    {
        private readonly IUnitOfWork work;
        private readonly IWebHostEnvironment _env;
        private readonly EcommerceDbContext _context;

        public ProdService(IUnitOfWork unitOfWork, IWebHostEnvironment env, EcommerceDbContext context)
        {
            work = unitOfWork;
            _env = env;
            _context = context;
        }

        public async Task<ProductDto> AddProductAsync(NewProductDto productDto)
        {
            if (productDto == null)
                throw new ArgumentNullException(nameof(productDto));

            // Auto-create default variant if none provided
            if (productDto.Variants == null || !productDto.Variants.Any())
            {
                productDto.Variants = new List<NewProductVariantDto>
                {
                    new NewProductVariantDto
                    {
                        Price = productDto.Price,
                        Quantity = 100,
                        Color = "Default",
                        Size = "One Size"
                    }
                };
            }

            if (string.IsNullOrWhiteSpace(productDto.CategoryName))
                throw new ArgumentException("CategoryName is required.", nameof(productDto));

            // Validate field lengths to match entity constraints
            if (string.IsNullOrWhiteSpace(productDto.Name))
                throw new ArgumentException("Product name is required.", nameof(productDto));

            if (productDto.Name.Length > 300)
                throw new ArgumentException("Product name must be 300 characters or less.", nameof(productDto));

            if (string.IsNullOrWhiteSpace(productDto.Description))
                throw new ArgumentException("Product description is required.", nameof(productDto));

            if (productDto.Description.Length > 200)
                throw new ArgumentException("Product description must be 200 characters or less.", nameof(productDto));

            // Resolve category by name and ensure CategoryId is set
            var category = await work.Categories.GetByNameAsync(productDto.CategoryName);
            if (category == null)
            {
                category = new Category { 
                    Name = productDto.CategoryName,
                    Description = $"Auto-generated category for {productDto.CategoryName}"
                };
                await work.Categories.AddAsync(category);
                await work.SaveChangesAsync();
            }

            // Create product first - WITHOUT any related entities
            var product = new Product
            {
                Name = productDto.Name,
                Description = productDto.Description,
                CategoryId = category.Id,
                Price = productDto.Price,
                IsOnSale = productDto.IsOnSale,
                SalePrice = productDto.SalePrice,
                ShippingCost = productDto.ShippingCost
            };

            await work.Products.AddAsync(product);
            await work.SaveChangesAsync();

            // Now add variants separately using direct context access
            if (productDto.Variants != null && productDto.Variants.Any())
            {
                foreach (var v in productDto.Variants)
                {
                    var variant = new ProductVariant
                    {
                        ProductId = product.Id,
                        Price = v.Price,
                        Quantity = v.Quantity,
                        Color = v.Color ?? "Default",
                        Size = v.Size ?? "One Size"
                    };
                    await work.ProductVariants.AddAsync(variant);
                }
                await work.SaveChangesAsync();
            }

            // Add images separately
            if (productDto.Images != null && productDto.Images.Any())
            {
                foreach (var img in productDto.Images)
                {
                    var image = new ProductImage
                    {
                        ProductId = product.Id,
                        ImageUrl = img.ImageData
                    };
                    await work.ProductImages.AddAsync(image);
                }
                await work.SaveChangesAsync();
            }

            // Reload product with all relationships
            var savedProduct = await work.Products.GetByIdAsync(product.Id);

            return MapToDto(savedProduct!);
        }

        public async Task<List<ProductDto>> GetAllProductByCategoryNameAsync(string categoryName)
        {
            if (string.IsNullOrWhiteSpace(categoryName))
                return new List<ProductDto>();

            var products = await work.Products.GetProductsByCategoryAsync(categoryName);

            if (products == null || !products.Any())
                return new List<ProductDto>();

            return products.Select(MapToDto).ToList();
        }

        public async Task<List<ProductDto>> GetAllProductsAsync()
        {
            var products = await work.Products.GetAllWithIncludesAsync();
            if (products == null || !products.Any())
                return new List<ProductDto>();

            return products.Select(MapToDto).ToList();
        }

        public async Task<ProductDto?> GetProductBySearchAsync(string search)
        {
            if (string.IsNullOrWhiteSpace(search))
                return null;

            var product = await work.Products.GetProductBySearchAsync(search);
            if (product == null)
                return null;

            return MapToDto(product);
        }

        public async Task<ProductDto?> GetProductByIdAsync(int productId)
        {
            var product = await work.Products.GetByIdAsync(productId);
            if (product == null)
                return null;

            return MapToDto(product);
        }

        public async Task<bool> RemoveProductAsync(int productId)
        {
            var product = await work.Products.GetByIdAsync(productId);
            if (product == null)
                return false;

            await work.Products.DeleteAsync(product);
            await work.SaveChangesAsync();
            return true;
        }

        public async Task<bool> UpdateProductAsync(int productId, ProductDto newProduct)
        {
            if (newProduct == null)
                throw new ArgumentNullException(nameof(newProduct));

            var product = await work.Products.GetByIdAsync(productId);
            if (product == null)
                return false;

            product.Name = newProduct.Name;
            product.Description = newProduct.Description;
            product.Price = newProduct.Price;
            product.IsOnSale = newProduct.IsOnSale;
            product.SalePrice = newProduct.SalePrice;

            // Update category if CategoryName provided
            if (!string.IsNullOrWhiteSpace(newProduct.CategoryName))
            {
                var category = await work.Categories.GetByNameAsync(newProduct.CategoryName);
                if (category == null)
                {
                    category = new Category { 
                        Name = newProduct.CategoryName,
                        Description = $"Auto-generated category for {newProduct.CategoryName}"
                    };
                    await work.Categories.AddAsync(category);
                    await work.SaveChangesAsync();
                }
                product.CategoryId = category.Id;
                product.Category = category;
            }

            // Replace Variants - IMPORTANT: Clear and recreate to avoid adding duplicates
            if (newProduct.Variants != null && newProduct.Variants.Any())
            {
                // Remove existing variants from database to avoid duplicates
                if (product.Variants != null && product.Variants.Any())
                {
                    var existingVariants = product.Variants.ToList();
                    foreach (var oldVariant in existingVariants)
                    {
                        await work.ProductVariants.DeleteAsync(oldVariant);
                    }
                    await work.SaveChangesAsync();
                }

                product.Variants = new List<ProductVariant>();

                foreach (var v in newProduct.Variants)
                {
                    // Create new variant - stock is SET to the new value, not added
                    var newVariant = new ProductVariant
                    {
                        ProductId = product.Id,
                        Price = v.Price,
                        Quantity = v.Quantity, // This SETS the quantity to the new value
                        Color = v.Color,
                        Size = v.Size
                    };
                    product.Variants.Add(newVariant);
                }
            }

            // Replace Images ONLY if new images are explicitly provided
            // If Images property is null or empty, keep existing images unchanged
            if (newProduct.Images != null && newProduct.Images.Any())
            {
                // Clear existing images (EF will handle cascade delete)
                if (product.Images != null && product.Images.Any())
                {
                    product.Images.Clear();
                }
                else
                {
                    product.Images = new List<ProductImage>();
                }

                foreach (var img in newProduct.Images)
                {
                    // If it's a base64 DataURL, save to disk and store URL path instead
                    var imageUrl = img.ImageData;
                    if (!string.IsNullOrEmpty(imageUrl) && imageUrl.StartsWith("data:"))
                    {
                        imageUrl = await SaveBase64ImageAsync(imageUrl);
                    }

                    var newImage = new ProductImage
                    {
                        ProductId = product.Id,
                        ImageUrl = imageUrl
                    };
                    product.Images.Add(newImage);
                }
            }

            await work.SaveChangesAsync();
            return true;
        }

        public async Task<PagedResult<ProductDto>> GetPagedProductsAsync(
            PaginationParams paginationParams,
            string? categoryName = null,
            string? search = null)
        {
            var (products, totalCount) = await work.Products.GetPagedProductsAsync(paginationParams, categoryName, search);

            var productDtos = products.Select(MapToDto).ToList();

            return new PagedResult<ProductDto>(
                paginationParams.PageNumber,
                paginationParams.PageSize,
                totalCount,
                productDtos
            );
        }

        // Mapping
        private ProductDto MapToDto(Product product)
        {
            var variantDtos = product.Variants?.Select(v => new NewProductVariantDto
            {
                Id = v.Id,
                Price = v.Price,
                Quantity = v.Quantity,
                Color = v.Color,
                Size = v.Size
            }).ToList() ?? new List<NewProductVariantDto>();

            var imageDtos = product.Images?.Select(img => new NewProductImageDto
            {
                ImageData = img.ImageUrl
            }).ToList() ?? new List<NewProductImageDto>();

            return new ProductDto
            {
                Id = product.Id,
                Name = product.Name,
                Description = product.Description,
                CategoryName = product.Category?.Name ?? "",
                Price = product.Price,
                IsOnSale = product.IsOnSale,
                SalePrice = product.SalePrice,
                Variants = variantDtos,
                Images = imageDtos
            };
        }

        /// <summary>
        /// Decodes a base64 DataURL (data:image/jpeg;base64,...) and saves it to
        /// wwwroot/uploads/products/, returning the public URL path.
        /// </summary>
        public async Task<(int converted, int failed)> MigrateBase64ImagesToFilesAsync()
        {
            // Load only IDs to avoid pulling 32MB of base64 into memory at once
            var ids = await _context.ProductImages
                .Where(i => i.ImageUrl != null && i.ImageUrl.StartsWith("data:"))
                .Select(i => i.Id)
                .ToListAsync();

            int converted = 0, failed = 0;
            foreach (var id in ids)
            {
                try
                {
                    var img = await _context.ProductImages.FindAsync(id);
                    if (img == null) continue;

                    var filePath = await SaveBase64ImageAsync(img.ImageUrl!);
                    img.ImageUrl = filePath;
                    await _context.SaveChangesAsync();

                    // Detach to free the large base64 string from memory
                    _context.Entry(img).State = Microsoft.EntityFrameworkCore.EntityState.Detached;
                    converted++;
                }
                catch
                {
                    failed++;
                }
            }

            return (converted, failed);
        }

        private async Task<string> SaveBase64ImageAsync(string dataUrl)
        {
            // dataUrl format: "data:<mime>;base64,<data>"
            var commaIdx = dataUrl.IndexOf(',');
            if (commaIdx < 0) return dataUrl; // not valid DataURL, return as-is

            var meta = dataUrl[..commaIdx];          // e.g. "data:image/jpeg;base64"
            var base64Data = dataUrl[(commaIdx + 1)..];
            var bytes = Convert.FromBase64String(base64Data);

            // Determine extension from MIME
            var ext = ".jpg";
            if (meta.Contains("png"))  ext = ".png";
            else if (meta.Contains("webp")) ext = ".webp";
            else if (meta.Contains("svg"))  ext = ".svg";

            var fileName = $"product_{Guid.NewGuid():N}{ext}";
            var uploadsDir = Path.Combine(
                _env.WebRootPath ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot"),
                "uploads", "products");
            Directory.CreateDirectory(uploadsDir);

            var filePath = Path.Combine(uploadsDir, fileName);
            await File.WriteAllBytesAsync(filePath, bytes);

            return $"/uploads/products/{fileName}";
        }
    }
}
