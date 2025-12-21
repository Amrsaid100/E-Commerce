using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace E_Commerce.Entities
{
    public class Order
    {
        [Key]
        public int OrderId { get; set; }
        public int UserId { get; set; }
        [Required]
        public string? Email { get; set; }
        [Required]
        public string? City { get; set; }
        [Required]
        public string? Street { get; set; }
        [Required]
        public string? PhoneNumber { get; set; }
        [Required]
        public decimal TotalAmount { get; set; }
        public List<OrderItem>? Items { get; set; }
        [Required]
        [ForeignKey("User")]
        public User? User { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    }
}
