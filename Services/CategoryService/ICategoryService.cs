using E_Commerce.Dtos.CategoryDtos;

namespace E_Commerce.Services.CategoryService
{
    public interface ICategoryService
    {
        Task<List<CategoryDto>> GetAllCategoriesAsync();

        Task<CategoryDto?> GetCategoryByIdAsync(int categoryId);

        Task<CategoryDto?> GetCategoryByNameAsync(string categoryName);

        Task<bool> AddCategoryAsync(NewCategoryDto categoryDto);

        Task<bool> UpdateCategoryAsync(int categoryId, UpdateCategoryDto categoryDto);

        Task<bool> DeleteCategoryAsync(int categoryId);

        Task<CategoryWithProductCountDto?> GetCategoryWithProductCountAsync(string categoryName);

    }
}
