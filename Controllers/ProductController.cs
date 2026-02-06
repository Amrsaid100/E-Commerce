using E_Commerce.Dtos.Helpers;
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

        // GET: /api/product?pageNumber=1&pageSize=12&categoryName=Men&search=shirt
        // Supports pagination, filtering by category, and search
        [HttpGet]
        public async Task<IActionResult> GetProducts(
            [FromQuery] PaginationParams paginationParams,
            [FromQuery] string? categoryName,
            [FromQuery] string? search)
        {
            try
            {
                var pagedResult = await _prodService.GetPagedProductsAsync(paginationParams, categoryName, search);

                if (pagedResult.Data == null || !pagedResult.Data.Any())
                {
                    return Ok(new PagedResult<ProductDto>(
                        paginationParams.PageNumber,
                        paginationParams.PageSize,
                        0,
                        new List<ProductDto>()
                    ));
                }

                return Ok(pagedResult);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error retrieving products", error = ex.Message });
            }
        }

        // GET: /api/product/{id}
        // Get a single product by ID
        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetProductById(int id)
        {
            try
            {
                var product = await _prodService.GetProductByIdAsync(id);
                if (product == null)
                    return NotFound(new { message = $"Product with ID {id} not found." });

                return Ok(product);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error retrieving product", error = ex.Message });
            }
        }

        // GET: /api/product/all
        // Admin/Owner listing endpoint to see all products (without pagination)
        [HttpGet("all")]
        [Authorize(Roles = "Admin,Owner")]
        public async Task<IActionResult> GetAll()
        {
            try
            {
                var items = await _prodService.GetAllProductsAsync();
                return Ok(items);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error retrieving all products", error = ex.Message });
            }
        }

        [HttpPost]
        [Authorize(Roles = "Admin,Owner")]
        public async Task<IActionResult> AddProduct([FromForm] NewProductFormDto productForm)
        {
            try
            {
                Console.WriteLine($"📦 Product creation request received:");
                Console.WriteLine($"   Name: {productForm?.Name}");
                Console.WriteLine($"   Description: {productForm?.Description}");
                Console.WriteLine($"   CategoryName: {productForm?.CategoryName}");
                Console.WriteLine($"   Price: {productForm?.Price}");
                Console.WriteLine($"   Images count: {productForm?.Images?.Count ?? 0}");

                if (!ModelState.IsValid)
                {
                    var errors = ModelState.Values.SelectMany(v => v.Errors);
                    Console.WriteLine("❌ ModelState is invalid:");
                    foreach (var error in errors)
                    {
                        Console.WriteLine($"   - {error.ErrorMessage}");
                    }
                    return BadRequest(new { message = "Invalid data", errors = errors.Select(e => e.ErrorMessage) });
                }

                if (string.IsNullOrWhiteSpace(productForm.Name) ||
                    string.IsNullOrWhiteSpace(productForm.Description) ||
                    string.IsNullOrWhiteSpace(productForm.CategoryName) ||
                    productForm.Price <= 0)
                {
                    Console.WriteLine("❌ Required fields validation failed");
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

                Console.WriteLine($"   Variant: {variant.Price}, Qty: {variant.Quantity}, Color: {variant.Color}, Size: {variant.Size}");

                // Convert uploaded files to Base64
                if (productForm.Images != null && productForm.Images.Count > 0)
                {
                    Console.WriteLine($"📸 Processing {productForm.Images.Count} images...");
                    foreach (var file in productForm.Images)
                    {
                        if (file.Length > 5242880) // 5MB limit
                        {
                            Console.WriteLine($"❌ Image file {file.FileName} exceeds 5MB limit");
                            return BadRequest("Image file size exceeds 5MB limit");
                        }

                        using (var ms = new MemoryStream())
                        {
                            await file.CopyToAsync(ms);
                            var base64 = "data:" + file.ContentType + ";base64," + Convert.ToBase64String(ms.ToArray());
                            product.Images.Add(new NewProductImageDto { ImageData = base64 });
                            Console.WriteLine($"   ✅ Processed image: {file.FileName} ({file.Length} bytes)");
                        }
                    }
                }

                Console.WriteLine("🔄 Calling AddProductAsync...");
                var created = await _prodService.AddProductAsync(product);
                Console.WriteLine($"✅ Product created successfully with ID: {created.Id}");
                
                return CreatedAtAction(nameof(GetProductById), new { id = created.Id }, created);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Product Creation Error: {ex.Message}");
                Console.WriteLine($"❌ Stack Trace: {ex.StackTrace}");
                if (ex.InnerException != null)
                {
                    Console.WriteLine($"❌ Inner Exception: {ex.InnerException.Message}");
                }
                return StatusCode(500, new { message = "Error creating product", error = ex.Message, innerError = ex.InnerException?.Message });
            }
        }

        [HttpPut("{id:int}")]
        [Authorize(Roles = "Admin,Owner")]
        public async Task<IActionResult> UpdateProduct(int id, [FromBody] ProductDto productDto)
        {
            Console.WriteLine($"📝 Product update request for ID: {id}");
            Console.WriteLine($"   Name: {productDto?.Name}");
            Console.WriteLine($"   Description: {productDto?.Description}");
            Console.WriteLine($"   Price: {productDto?.Price}");
            Console.WriteLine($"   Variants count: {productDto?.Variants?.Count ?? 0}");
            Console.WriteLine($"   Images count: {productDto?.Images?.Count ?? 0}");
            
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage);
                Console.WriteLine("❌ ModelState is invalid:");
                foreach (var error in errors)
                {
                    Console.WriteLine($"   - {error}");
                }
                return BadRequest(new { message = "Invalid data", errors });
            }

            // Only validate required fields
            if (string.IsNullOrWhiteSpace(productDto.Name) || 
                string.IsNullOrWhiteSpace(productDto.Description) || 
                productDto.Price <= 0)
            {
                Console.WriteLine("❌ Required fields validation failed");
                return BadRequest(new { message = "Name, Description and Price are required and must be valid" });
            }

            try
            {
                var updated = await _prodService.UpdateProductAsync(id, productDto);
                if (!updated)
                {
                    Console.WriteLine($"❌ Product {id} not found");
                    return NotFound(new { message = "Product not found." });
                }

                var updatedProduct = await _prodService.GetProductByIdAsync(id);
                Console.WriteLine($"✅ Product {id} updated successfully");

                if (updatedProduct != null)
                    return Ok(updatedProduct);
                else
                    return Ok(new { message = "Product updated successfully" });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Product Update Error: {ex.Message}");
                Console.WriteLine($"❌ Stack Trace: {ex.StackTrace}");
                if (ex.InnerException != null)
                {
                    Console.WriteLine($"❌ Inner Exception: {ex.InnerException.Message}");
                }
                return StatusCode(500, new { message = "Error updating product", error = ex.Message, innerError = ex.InnerException?.Message });
            }
        }

        [HttpDelete("{id:int}")]
        [Authorize(Roles = "Admin,Owner")]
        public async Task<IActionResult> RemoveProduct(int id)
        {
            try
            {
                var removed = await _prodService.RemoveProductAsync(id);
                if (!removed)
                    return NotFound(new { message = "Product not found." });

                return NoContent();
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error deleting product", error = ex.Message });
            }
        }
    }
}
