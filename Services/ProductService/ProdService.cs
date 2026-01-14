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

            if (productDto.Variants == null || !productDto.Variants.Any())
                throw new ArgumentException("Product must have at least one variant.", nameof(productDto));

            if (string.IsNullOrWhiteSpace(productDto.CategoryName))
                throw new ArgumentException("CategoryName is required.", nameof(productDto));

            var variants = productDto.Variants.Select(v => new ProductVariant
            {
                Price = v.Price,
                Quantity = v.Quantity,
                Color = v.Color,
                Size = v.Size
            }).ToList();

            // Resolve category by name and ensure CategoryId is set
            var category = await work.Categories.GetByNameAsync(productDto.CategoryName);
            if (category == null)
            {
                category = new Category { Name = productDto.CategoryName };
                await work.Categories.AddAsync(category);
                await work.SaveChangesAsync();
            }

            var product = new Product
            {
                Name = productDto.Name,  // إضافة هذا السطر
                Description = productDto.Description,
                CategoryId = category.Id,
                Category = category,
                Price = productDto.Price,
                Variants = variants
            };

            // Images ( NewProductDto)
            if (productDto.Images != null && productDto.Images.Any())
            {
                product.Images = productDto.Images.Select(img => new ProductImage
                {
                    ImageUrl = img.ImageData // Store Base64 data
                }).ToList();
            }

            await work.Products.AddAsync(product);
            await work.SaveChangesAsync();

            return MapToDto(product);
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

            product.Name = newProduct.Name;  // إضافة هذا السطر
            product.Description = newProduct.Description;
            product.Price = newProduct.Price;

            // Update category if CategoryName provided
            if (!string.IsNullOrWhiteSpace(newProduct.CategoryName))
            {
                var category = await work.Categories.GetByNameAsync(newProduct.CategoryName);
                if (category == null)
                {
                    category = new Category { Name = newProduct.CategoryName };
                    await work.Categories.AddAsync(category);
                    await work.SaveChangesAsync();
                }
                product.CategoryId = category.Id;
                product.Category = category;
            }

            // Replace Variants
            if (newProduct.Variants != null && newProduct.Variants.Any())
            {
                product.Variants ??= new List<ProductVariant>();
                product.Variants.Clear();

                foreach (var v in newProduct.Variants)
                {
                    product.Variants.Add(new ProductVariant
                    {
                        Price = v.Price,
                        Quantity = v.Quantity,
                        Color = v.Color,
                        Size = v.Size
                    });
                }
            }

            // Replace Images (only if new images provided)
            if (newProduct.Images != null && newProduct.Images.Any())
            {
                product.Images ??= new List<ProductImage>();
                product.Images.Clear();

                foreach (var img in newProduct.Images)
                {
                    product.Images.Add(new ProductImage
                    {
                        ImageUrl = img.ImageData // Store Base64 data
                    });
                }
            }

            await work.SaveChangesAsync();
            return true;
        }

        // Mapping
        private ProductDto MapToDto(Product product)
        {
            var variantDtos = product.Variants?.Select(v => new NewProductVariantDto
            {
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
                Variants = variantDtos,
                Images = imageDtos
            };
        }
    }
}
