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

        public virtual List<Product> Products { get; set; }
    }
}
