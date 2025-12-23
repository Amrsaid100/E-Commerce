using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace E_Commerce.Entities
{
    public class Cart
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public List<CartItem> Items { get; set; } = new();

        public decimal TotalPrice { get; set; }

        [ForeignKey(nameof(User))]
        public int UserId { get; set; }

        public User? User { get; set; }
    }
}