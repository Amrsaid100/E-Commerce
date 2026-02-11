// CategoryDto.cs
using E_Commerce.Entities;

namespace E_Commerce.Dtos.CategoryDtos
{
    public class CategoryDto
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string? Description { get; set; }
        public SizeType SizeType { get; set; }
        public int? MinSize { get; set; }
        public int? MaxSize { get; set; }
        public List<string>? AvailableSizes { get; set; }
    }

    public class NewCategoryDto
    {
        public string Name { get; set; }
        public string? Description { get; set; }
        public SizeType SizeType { get; set; } = SizeType.None;
        public int? MinSize { get; set; }
        public int? MaxSize { get; set; }
        public List<string>? AvailableSizes { get; set; }
    }

    public class UpdateCategoryDto
    {
        public string? Name { get; set; }
        public string? Description { get; set; }
        public SizeType? SizeType { get; set; }
        public int? MinSize { get; set; }
        public int? MaxSize { get; set; }
        public List<string>? AvailableSizes { get; set; }
    }

    public class CategoryWithProductCountDto
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string? Description { get; set; }
        public int ProductCount { get; set; }
        public SizeType SizeType { get; set; }
        public int? MinSize { get; set; }
        public int? MaxSize { get; set; }
        public List<string>? AvailableSizes { get; set; }
    }
}
