using E_Commerce.Dtos.CategoryDtos;
using E_Commerce.Entities;
using E_Commerce.UnitOfWork;

namespace E_Commerce.Services.CategoryService
{
    public class CategoryService : ICategoryService
    {
        private readonly IUnitOfWork work;

        public CategoryService(IUnitOfWork unitOfWork)
        {
            work = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        }

        // Bring the categories
        public async Task<List<CategoryDto>> GetAllCategoriesAsync()
        {
            var categories = await work.Categories.GetAllAsync();

            if (categories == null || !categories.Any())
                return new List<CategoryDto>();

            return categories.Select(c => MapToDto(c)).ToList();
        }

        //Bring the category By ID
        public async Task<CategoryDto?> GetCategoryByIdAsync(int categoryId)
        {
            if (categoryId <= 0)
                return null;

            var category = await work.Categories.GetByIdAsync(categoryId);

            if (category == null)
                return null;

            return MapToDto(category);
        }

        // Bring category By Name
        public async Task<CategoryDto?> GetCategoryByNameAsync(string categoryName)
        {
            if (string.IsNullOrWhiteSpace(categoryName))
                return null;

            var categories = await work.Categories.GetAllAsync();

            //  null check On categories
            if (categories == null || !categories.Any())
                return null;

            var category = categories.FirstOrDefault(c =>
                c.Name != null && c.Name.Equals(categoryName, StringComparison.OrdinalIgnoreCase));

            if (category == null)
                return null;

            return MapToDto(category);
        }

        // Add New category 
        public async Task<bool> AddCategoryAsync(NewCategoryDto categoryDto)
        {
            //  null check On categoryDto
            if (categoryDto == null)
                return false;

            if (string.IsNullOrWhiteSpace(categoryDto.Name))
                return false;

            //If the category Does not exist
            var existing = await GetCategoryByNameAsync(categoryDto.Name);
            if (existing != null)
                return false; //Already Exist

            var category = new Category
            {
                Name = categoryDto.Name,
                Description = categoryDto.Description
            };

            await work.Categories.AddAsync(category);
            await work.SaveChangesAsync();
            return true;
        }

        //Update category
        public async Task<bool> UpdateCategoryAsync(int categoryId, UpdateCategoryDto categoryDto)
        {
            if (categoryId <= 0)
                return false;

            //  null check on categoryDto Separatly
            if (categoryDto == null)
                return false;

            var category = await work.Categories.GetByIdAsync(categoryId);
            if (category == null)
                return false;

            // Update the Data
            if (!string.IsNullOrWhiteSpace(categoryDto.Name))
                category.Name = categoryDto.Name;

            if (!string.IsNullOrWhiteSpace(categoryDto.Description))
                category.Description = categoryDto.Description;

            await work.SaveChangesAsync();
            return true;
        }

        //Delete category
        public async Task<bool> DeleteCategoryAsync(int categoryId)
        {
            if (categoryId <= 0)
                return false;

            var category = await work.Categories.GetByIdAsync(categoryId);
            if (category == null)
                return false;

            //  make sure not exist any products link with this Category
            if (!string.IsNullOrWhiteSpace(category.Name))
            {
                var products = await work.Products.GetProductsByCategoryAsync(category.Name);
                if (products != null && products.Any())
                    return false; 
            }

            await work.Categories.DeleteAsync(category);
            await work.SaveChangesAsync();
            return true;
        }

        // Bring the Counter With category 
        public async Task<CategoryWithProductCountDto?> GetCategoryWithProductCountAsync(int categoryId)
        {
            if (categoryId <= 0)
                return null;

            var category = await work.Categories.GetByIdAsync(categoryId);
            if (category == null)
                return null;

            //  null check On category.Name
            if (string.IsNullOrWhiteSpace(category.Name))
                return null;

            // You Need To Bring Products repository
            var products = await work.Products.GetProductsByCategoryAsync(category.Name);

            return new CategoryWithProductCountDto
            {
                Id = category.Id,
                Name = category.Name,
                Description = category.Description,
                ProductCount = products?.Count ?? 0
            };
        }

        // Private method TO Transfer Entity To DTO
        private CategoryDto MapToDto(Category category)
        {
            //  defensive programming - null check
            if (category == null)
                throw new ArgumentNullException(nameof(category), "Category cannot be null");

            return new CategoryDto
            {
                Id = category.Id,
                Name = category.Name ?? string.Empty, 
                Description = category.Description
            };
        }
    }
}
