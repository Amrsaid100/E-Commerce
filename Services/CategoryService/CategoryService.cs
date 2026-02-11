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
                Description = categoryDto.Description,
                SizeType = categoryDto.SizeType,
                MinSize = categoryDto.MinSize,
                MaxSize = categoryDto.MaxSize,
                AvailableSizes = categoryDto.AvailableSizes != null && categoryDto.AvailableSizes.Any() 
                    ? string.Join(",", categoryDto.AvailableSizes) 
                    : null
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

            if (categoryDto.SizeType.HasValue)
                category.SizeType = categoryDto.SizeType.Value;

            if (categoryDto.MinSize.HasValue)
                category.MinSize = categoryDto.MinSize.Value;

            if (categoryDto.MaxSize.HasValue)
                category.MaxSize = categoryDto.MaxSize.Value;

            if (categoryDto.AvailableSizes != null)
                category.AvailableSizes = categoryDto.AvailableSizes.Any() 
                    ? string.Join(",", categoryDto.AvailableSizes) 
                    : null;

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

            // Get all products in this category
            var allProducts = await work.Products.GetAllAsync();
            var productsInCategory = allProducts.Where(p => p.CategoryId == categoryId).ToList();

            // Delete all products (and their variants/images will cascade)
            foreach (var product in productsInCategory)
            {
                // Delete product variants first
                var allVariants = await work.ProductVariants.GetAllAsync();
                var productVariants = allVariants.Where(v => v.ProductId == product.Id).ToList();
                foreach (var variant in productVariants)
                {
                    await work.ProductVariants.DeleteAsync(variant);
                }

                // Delete product images
                var allImages = await work.ProductImages.GetAllAsync();
                var productImages = allImages.Where(i => i.ProductId == product.Id).ToList();
                foreach (var image in productImages)
                {
                    await work.ProductImages.DeleteAsync(image);
                }

                // Delete the product itself
                await work.Products.DeleteAsync(product);
            }

            // Now delete the category
            await work.Categories.DeleteAsync(category);
            await work.SaveChangesAsync();
            
            return true;
        }

        // Bring the Counter With category 
        public async Task<CategoryWithProductCountDto?> GetCategoryWithProductCountAsync(string categoryName)
        {
            //  null or empty check
            if (string.IsNullOrWhiteSpace(categoryName))
                return null;

            // Bring all categories (because repo doesn't have GetByName)
            var categories = await work.Categories.GetAllAsync();

            if (categories == null || !categories.Any())
                return null;

            // Find category by name (case-insensitive)
            var category = categories.FirstOrDefault(c =>
                c.Name != null && c.Name.Equals(categoryName, StringComparison.OrdinalIgnoreCase));

            if (category == null)
                return null;

            // Bring products related to this category
            var products = await work.Products.GetProductsByCategoryAsync(category.Name);

            return new CategoryWithProductCountDto
            {
                Id = category.Id,
                Name = category.Name,
                Description = category.Description,
                ProductCount = products?.Count ?? 0,
                SizeType = category.SizeType,
                MinSize = category.MinSize,
                MaxSize = category.MaxSize,
                AvailableSizes = !string.IsNullOrWhiteSpace(category.AvailableSizes) 
                    ? category.AvailableSizes.Split(',', StringSplitOptions.RemoveEmptyEntries).ToList()
                    : null
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
                Description = category.Description,
                SizeType = category.SizeType,
                MinSize = category.MinSize,
                MaxSize = category.MaxSize,
                AvailableSizes = !string.IsNullOrWhiteSpace(category.AvailableSizes) 
                    ? category.AvailableSizes.Split(',', StringSplitOptions.RemoveEmptyEntries).ToList()
                    : null
            };
        }
    }
}
