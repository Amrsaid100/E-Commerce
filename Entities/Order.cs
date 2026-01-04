using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace E_Commerce.Entities
{
    public enum OrderStatus
    {
        PendingPayment,
        Paid,
        Failed
    }
    public class Order
    {
        [Key]
        public int Id { get; set; }

        [ForeignKey(nameof(User))]
        public int UserId { get; set; }

        [Required]
        public string Email { get; set; } = default!;

        [Required]
        public string City { get; set; } = default!;

        [Required]
        public string Street { get; set; } = default!;

        [Required]
        public string PhoneNumber { get; set; } = default!;

        [Required]
        public decimal TotalAmount { get; set; }

        public OrderStatus Status { get; set; }   

        public string? PaymentReference { get; set; }

        public List<OrderItem> Items { get; set; } = new();

        public User? User { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}