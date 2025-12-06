using System.ComponentModel.DataAnnotations;

namespace E_Commerce.Entities
{
    public class User
    {
        [Key]
        public int Id { get; set; } 

        [Required]
        [EmailAddress]
        [MaxLength(100)]
        public string Email { get; set; }
        [MaxLength(100)]
        public string Name { get; set; }
        public Cart Cart { get; set; }
        public List<Order> Orders { get; set; }

    }
}
