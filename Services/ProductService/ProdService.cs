using E_Commerce.Dtos.Helpers;
using E_Commerce.Dtos.ProductDtos;
using E_Commerce.Entities;
using E_Commerce.UnitOfWork;

namespace E_Commerce.Services.ProductService
{
    public class ProdService : IProductService
    {
        private readonly IUnitOfWork work;

        public ProdService(IUnitOfWork unitOfWork)
        {
            work = unitOfWork;
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
                    var newImage = new ProductImage
                    {
                        ProductId = product.Id,
                        ImageUrl = img.ImageData // Store Base64 data
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
    }
}
