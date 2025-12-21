// CategoryDto.cs
namespace E_Commerce.Dtos.CategoryDtos
{
    public class CategoryDto
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string? Description { get; set; }
    }

    public class NewCategoryDto
    {
        public string Name { get; set; }
        public string? Description { get; set; }
    }

    public class UpdateCategoryDto
    {
        public string? Name { get; set; }
        public string? Description { get; set; }
    }

    public class CategoryWithProductCountDto
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string? Description { get; set; }
        public int ProductCount { get; set; }
    }
}
