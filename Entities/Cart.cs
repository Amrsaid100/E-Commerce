using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace E_Commerce.Entities
{
    public class Cart
    {
        [Key]
        public int Id { get; set; }
        [Required]
        public  List<CartItem> Items { get; set; }
        public decimal TotalPrice { get; set; }
        [ForeignKey("User")]
        public int UserId { get; set; }
        
        public User User { get; set; }
    }
}
