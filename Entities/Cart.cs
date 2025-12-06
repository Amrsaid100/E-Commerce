using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace E_Commerce.Entities
{
    public class Cart
    {
        [Key]
        public int Id { get; set; }
        [ForeignKey("User")]
        public Guid? UserId { get; set; }
        public User User { get; set; }
        public virtual IEnumerable<CartItem> Items { get; set; }
    }
}
