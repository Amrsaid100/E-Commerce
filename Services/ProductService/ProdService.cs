using E_Commerce.Dtos.ProductDtos;
using E_Commerce.Entities;
using E_Commerce.UnitOfWork;
using Microsoft.IdentityModel.Tokens;
using System.Security.Principal;

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
            List<ProductVariant> variants = new List<ProductVariant>();
            foreach (var variantDto in productDto.Variants)
            {
                var variant = new ProductVariant
                {
                    Price = variantDto.Price,
                    Quantity = variantDto.Quantity,
                    Color = variantDto.Color,
                    Size = variantDto.Size
                };
                variants.Add(variant);
            }
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
            if(string.IsNullOrWhiteSpace(CategoryName))
                return null;    
            List<Product> Products = await work.Products.GetProductsByCategoryAsync(CategoryName);
            List<ProductDto> productDtos = new List<ProductDto>();
           

            foreach(var prod in Products)
            {
                List<NewProductVariantDto> ProductVariantDtos = new List<NewProductVariantDto>();
                List<NewProductImageDto> ImageDtos = new List<NewProductImageDto>();
                foreach (var variant in prod.Variants)
                {
                    var variantDto=new NewProductVariantDto
                    {
                        Price = variant.Price,
                        Quantity = variant.Quantity,
                        Color = variant.Color,
                        Size = variant.Size
                    };
                    ProductVariantDtos.Add(variantDto);
                }
                foreach(var img in prod.Images)
                {
                    var Image = new NewProductImageDto
                    {
                        ImageUrl = img.ImageUrl
                    };
                    ImageDtos.Add(Image);
                }

                var productDto = new ProductDto
                {
                    Description = prod.Description,
                    Price = prod.Price,
                    Variants = ProductVariantDtos,
                    Images = ImageDtos
                };
                productDtos.Add(productDto);
            }

            return productDtos;

        }

        public async Task<ProductDto> GetProudctBySearchAsync(string Search)
        {
            if (string.IsNullOrWhiteSpace(Search))
                return null;
            var Product = await work.Products.GetProductBySearchAsync(Search);
            if (Product == null)
                return null;
            List<NewProductVariantDto> variants = new List<NewProductVariantDto>();
            List<NewProductImageDto> ImageDtos = new List<NewProductImageDto>();
            foreach(var variant in Product.Variants)
            {
                NewProductVariantDto variantDto = new NewProductVariantDto()
                {
                    Price = variant.Price,
                    Quantity = variant.Quantity,
                    Color = variant.Color,
                    Size = variant.Size
                };
                variants.Add(variantDto);
            }
            foreach(var image in Product.Images)
            {
                NewProductImageDto imageDto = new NewProductImageDto()
                { 
                     ImageUrl=image.ImageUrl
                };


                ImageDtos.Add(imageDto);
            }
            ProductDto ProductSearch = new ProductDto()
            {
                Description = Product.Description,
                Price = Product.Price,
                Variants = variants,
                Images = ImageDtos
            };
            
            return ProductSearch;
        }

        public async Task RemoveProductAsync(int productId)
        {
            var product =await work.Products.GetByIdAsync(productId);
            if (product == null)
                return;
            await work.Products.DeleteAsync(product);
            await work.SaveChangesAsync();
        }

        public async Task UpdateProductAsync(int ProductId , ProductDto NewProduct)
        {
            var product = await work.Products.GetByIdAsync(ProductId);
            if (product == null)
                return;

            product.Description = NewProduct.Description;
            product.Price = NewProduct.Price;
            
            await work.SaveChangesAsync(); 
        }
    }
}
