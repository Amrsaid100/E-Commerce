using E_Commerce.Dtos.CategoryDtos;
using E_Commerce.Entities;
using E_Commerce.Services.CategoryService;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace E_Commerce.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CategoryController : ControllerBase
    {
        private readonly ICategoryService categoryService;

        public CategoryController(ICategoryService categoryService)
        {
            this.categoryService = categoryService;
        }

        // Get all categories
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var categories = await categoryService.GetAllCategoriesAsync();
            return Ok(categories);
        }

        // Get category by ID
        [HttpGet("id/{id:int}")]
        public async Task<IActionResult> GetById(int id)
        {
            var category = await categoryService.GetCategoryByIdAsync(id);
            if (category == null)
                return NotFound();

            return Ok(category);
        }

        // Get category by Name
        [HttpGet("name/{categoryName}")]
        public async Task<IActionResult> GetByName(string categoryName)
        {
            var category = await categoryService.GetCategoryByNameAsync(categoryName);
            if (category == null)
                return NotFound();

            return Ok(category);
        }

        // Get category with products count (by Name)
        [HttpGet("name/{categoryName}/products/count")]
        public async Task<IActionResult> GetCategoryWithProductCount(string categoryName)
        {
            var result = await categoryService.GetCategoryWithProductCountAsync(categoryName);
            if (result == null)
                return NotFound();

            return Ok(result);
        }

        // Create new category
        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Create(NewCategoryDto newCategory)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var created = await categoryService.AddCategoryAsync(newCategory);
            if (!created)
                return BadRequest("Category already exists");

            return StatusCode(201);
        }

        // Update category
        [HttpPut("id/{id:int}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Update(int id, UpdateCategoryDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var updated = await categoryService.UpdateCategoryAsync(id, dto);
            if (!updated)
                return NotFound();

            return NoContent();
        }

        // Delete category
        [HttpDelete("id/{id:int}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(int id)
        {
            var deleted = await categoryService.DeleteCategoryAsync(id);
            if (!deleted)
                return NotFound();

            return NoContent();
        }
    }
}
//the route should be as 🔽 
//GET /api/category/name/electronics
//GET / api / category / name / electronics / products / count
//GET / api / category / id / 5
