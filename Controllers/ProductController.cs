using E_Commerce.Dtos.ProductDtos;
using E_Commerce.Services.ProductService;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace E_Commerce.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ProductController : ControllerBase
    {
        private readonly IProductService ProdService;

        public ProductController(IProductService ProdService)
        {
            this.ProdService = ProdService;
        }

        [HttpGet]
        public async Task<IActionResult> GetProduct(string categoryname)
        {
            var products = await ProdService.GetAllProductByCategoryNameAsync(categoryname);
            if (products == null || !products.Any())
            {
                return NotFound(new { message = "No products found for the specified category." });
            }
            return Ok(products);
        }

        [HttpGet]
        public async Task<IActionResult> GetProd(string productname)
        {
            var product = await ProdService.GetProductBySearchAsync(productname);
            if (product == null)
            {
                return NotFound(new { message = "Product not found." });
            }
            return Ok(product);
        }

        [HttpPost]
        [Authorize(Roles ="Admin")]
        public async Task<IActionResult> AddProduct(NewProductDto product)
        {
            if (ModelState.IsValid)
            {
                await ProdService.AddProductAsync(product);
                return CreatedAtAction(nameof(GetProd), new { productname = product.Description }, product);
            }
            return BadRequest(ModelState);

        }

        [HttpPost("update/{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> UpdateProduct(int id,ProductDto product)
        {
            if (ModelState.IsValid)
            {

               var updated= await ProdService.UpdateProductAsync(id, product);
                if(updated)
                {
                    return NoContent();
                }
                return NotFound();
            }
            return BadRequest(ModelState);
        }


        [HttpPost("remove/{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> RemoveProduct(int id)
        {
            await ProdService.RemoveProductAsync(id);
            return NoContent();
        }
    }
}
