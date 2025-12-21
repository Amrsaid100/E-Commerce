using System.ComponentModel.DataAnnotations;
using System.Drawing;

namespace E_Commerce.Dtos.UserDto
{
    public class UserProductVariantDto
    {
        [Required]
        public int ProductVariantId { get; set; }
        [Required]
        public string Description { get; set; }
        public decimal Price { get; set; }  
        public string Image { get; set; }
        public String size { get; set; }
        public int quantity { get; set; }
    }
}
