using E_Commerce.Dtos.ProductDtos;
using E_Commerce.Services.ProductService;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace E_Commerce.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ProductController : ControllerBase
    {
        private readonly IProductService _prodService;

        public ProductController(IProductService prodService)
        {
            _prodService = prodService;
        }

        // GET: /api/product?categoryName=Men  OR  /api/product?search=shirt  OR  /api/product (all)
        [HttpGet]
        public async Task<IActionResult> GetProducts([FromQuery] string? categoryName, [FromQuery] string? search)
        {
            if (!string.IsNullOrWhiteSpace(search))
            {
                var product = await _prodService.GetProductBySearchAsync(search);
                if (product == null)
                    return NotFound(new { message = "Product not found." });

                return Ok(product);
            }

            if (!string.IsNullOrWhiteSpace(categoryName))
            {
                var products = await _prodService.GetAllProductByCategoryNameAsync(categoryName);
                if (products == null || !products.Any())
                    return NotFound(new { message = "No products found for the specified category." });

                return Ok(products);
            }

            // If no params, return all products
            var allProducts = await _prodService.GetAllProductsAsync();
            return Ok(allProducts);
        }

        // GET: /api/product/all
        // Admin/Owner listing endpoint to see all products
        [HttpGet("all")]
        // [Authorize(Roles = "Admin,Owner")]  // Temporarily disabled for debugging
        public async Task<IActionResult> GetAll()
        {
            var items = await _prodService.GetAllProductsAsync();
            return Ok(items);
        }

        [HttpPost]
        [Authorize(Roles = "Admin,Owner")]
        public async Task<IActionResult> AddProduct([FromForm] NewProductFormDto productForm)
        {
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values.SelectMany(v => v.Errors);
                return BadRequest(new { message = "Invalid data", errors = errors.Select(e => e.ErrorMessage) });
            }

            if (string.IsNullOrWhiteSpace(productForm.Name) ||  // إضافة هذا الشرط
                string.IsNullOrWhiteSpace(productForm.Description) || 
                string.IsNullOrWhiteSpace(productForm.CategoryName) || 
                productForm.Price <= 0)
            {
                return BadRequest(new { message = "Name, Description, CategoryName, and Price are required" });
            }

            var product = new NewProductDto
            {
                Name = productForm.Name,
                Description = productForm.Description,
                CategoryName = productForm.CategoryName,
                Price = productForm.Price,
                ShippingCost = productForm.ShippingCost,
                Variants = new List<NewProductVariantDto>(),
                Images = new List<NewProductImageDto>()
            };

            // Create a single variant from form data
            var variant = new NewProductVariantDto
            {
                Price = productForm.VariantPrice > 0 ? productForm.VariantPrice : productForm.Price,
                Quantity = productForm.VariantQuantity > 0 ? productForm.VariantQuantity : 10,
                Color = productForm.VariantColor ?? "Default",
                Size = productForm.VariantSize ?? "One Size"
            };
            product.Variants.Add(variant);

            // Convert uploaded files to Base64
            if (productForm.Images != null && productForm.Images.Count > 0)
            {
                foreach (var file in productForm.Images)
                {
                    if (file.Length > 5242880) // 5MB limit
                        return BadRequest("Image file size exceeds 5MB limit");

                    using (var ms = new MemoryStream())
                    {
                        await file.CopyToAsync(ms);
                        var base64 = "data:" + file.ContentType + ";base64," + Convert.ToBase64String(ms.ToArray());
                        product.Images.Add(new NewProductImageDto { ImageData = base64 });
                    }
                }
            }

            try
            {
                var created = await _prodService.AddProductAsync(product);
                return Ok(created);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error creating product", error = ex.Message });
            }
        }

        [HttpPut("{id:int}")]
        [Authorize(Roles = "Admin,Owner")]
        public async Task<IActionResult> UpdateProduct(int id, [FromBody] ProductDto productDto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            if (string.IsNullOrWhiteSpace(productDto.Description) || productDto.Price <= 0)
                return BadRequest(new { message = "Description and Price are required" });

            try
            {
                var updated = await _prodService.UpdateProductAsync(id, productDto);
                if (!updated)
                    return NotFound(new { message = "Product not found." });

                var updatedProduct = await _prodService.GetAllProductsAsync();
                var result = updatedProduct.FirstOrDefault(p => p.Id == id);
                
                if (result != null)
                    return Ok(result);
                else
                    return Ok(new { message = "Product updated successfully" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error updating product", error = ex.Message });
            }
        }

        [HttpDelete("{id:int}")]
        [Authorize(Roles = "Admin,Owner")]
        public async Task<IActionResult> RemoveProduct(int id)
        {
            var removed = await _prodService.RemoveProductAsync(id);
            if (!removed)
                return NotFound(new { message = "Product not found." });

            return NoContent();
        }
    }
}
