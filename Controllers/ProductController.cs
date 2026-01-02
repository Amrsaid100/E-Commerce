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

        // GET: /api/product?categoryName=Men  OR  /api/product?search=shirt
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

            return BadRequest(new
            {
                message = "Provide either 'categoryName' or 'search' query parameter. Example: /api/product?categoryName=Men OR /api/product?search=shirt"
            });
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> AddProduct([FromBody] NewProductDto product)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var created = await _prodService.AddProductAsync(product);
            return Ok(created);
        }

        [HttpPut("{id:int}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> UpdateProduct(int id, [FromBody] ProductDto product)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var updated = await _prodService.UpdateProductAsync(id, product);
            if (!updated)
                return NotFound(new { message = "Product not found." });

            return NoContent();
        }

        [HttpDelete("{id:int}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> RemoveProduct(int id)
        {
            var removed = await _prodService.RemoveProductAsync(id);
            if (!removed)
                return NotFound(new { message = "Product not found." });

            return NoContent();
        }
    }
}
