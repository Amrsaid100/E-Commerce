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

        public async Task AddProductAsync(NewProductDto productDto)
        {
            if (productDto == null)
                return;

            //Add null check Variants
            if (productDto.Variants == null || !productDto.Variants.Any())
                return;

            var variants = productDto.Variants.Select(variantDto => new ProductVariant
            {
                Price = variantDto.Price,
                Quantity = variantDto.Quantity,
                Color = variantDto.Color,
                Size = variantDto.Size
            }).ToList();

            var product = new Product
            {
                Description = productDto.Description,
                Category = productDto.Category,
                Price = productDto.Price,
                Variants = variants
            };

            await work.Products.AddAsync(product);
            await work.SaveChangesAsync();
        }

        public async Task<List<ProductDto>> GetAllProductByCategoryNameAsync(string CategoryName)
        {
            if (string.IsNullOrWhiteSpace(CategoryName))
                return new List<ProductDto>();

            var products = await work.Products.GetProductsByCategoryAsync(CategoryName);

            if (products == null || !products.Any())
                return new List<ProductDto>();

            // use mapping method 
            return products.Select(p => MapToDto(p)).ToList();
        }

        public async Task<ProductDto?> GetProductBySearchAsync(string Search)
        {
            if (string.IsNullOrWhiteSpace(Search))
                return null;

            var product = await work.Products.GetProductBySearchAsync(Search);
            if (product == null)
                return null;

            // use mapping method
            return MapToDto(product);
        }

        public async Task RemoveProductAsync(int productId)
        {
            var product = await work.Products.GetByIdAsync(productId);
            if (product == null)
                return;

            await work.Products.DeleteAsync(product);
            await work.SaveChangesAsync();
        }

        public async Task UpdateProductAsync(int ProductId, ProductDto NewProduct)
        {
            var product = await work.Products.GetByIdAsync(ProductId);
            if (product == null)
                return;

            product.Description = NewProduct.Description;
            product.Price = NewProduct.Price;

            // Update the Variants
            if (NewProduct.Variants != null && NewProduct.Variants.Any())
            {
                product.Variants.Clear();
                foreach (var varDto in NewProduct.Variants)
                {
                    product.Variants.Add(new ProductVariant
                    {
                        Price = varDto.Price,
                        Quantity = varDto.Quantity,
                        Color = varDto.Color,
                        Size = varDto.Size
                    });
                }
            }

            // Update the Images
            if (NewProduct.Images != null && NewProduct.Images.Any())
            {
                product.Images.Clear();
                foreach (var imgDto in NewProduct.Images)
                {
                    product.Images.Add(new ProductImage
                    {
                        ImageUrl = imgDto.ImageUrl
                    });
                }
            }

            await work.SaveChangesAsync();
        }

        // Private method To change Product To ProductDto
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
                ImageUrl = img.ImageUrl
            }).ToList() ?? new List<NewProductImageDto>();

            return new ProductDto
            {
                Description = product.Description,
                Price = product.Price,
                Variants = variantDtos,
                Images = imageDtos
            };
        }
    }
}
