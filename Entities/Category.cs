using System.ComponentModel.DataAnnotations;

namespace E_Commerce.Entities
{
    public class Category
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [MaxLength(100)]
        public string Name { get; set; }

        
        [MaxLength(500)]
        public string? Description { get; set; }

        // نوع المقاسات لهذه الفئة
        public SizeType SizeType { get; set; } = SizeType.None;

        // نطاق المقاسات الرقمية (اختياري)
        public int? MinSize { get; set; }
        public int? MaxSize { get; set; }

        // المقاسات المتاحة (للـ Clothing أو مقاسات مخصصة)
        [MaxLength(200)]
        public string? AvailableSizes { get; set; } // مثال: "S,M,L,XL,XXL,XXXL"

        public virtual List<Product> Products { get; set; } = new List<Product>();
    }
}
