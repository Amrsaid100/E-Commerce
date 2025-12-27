using E_Commerce.Dtos.Roles;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace E_Commerce.Entities
{
    public class User
    {
        [Key]
        public int Id { get; set; }

        [Required, EmailAddress, MaxLength(100)]
        public string Email { get; set; }

        [Required, MaxLength(200)]
        public string PasswordHash { get; set; }

        [Required, MaxLength(50)]
        public UserRole Role { get; set; } = UserRole.User;


        [MaxLength(100)]
        public string Name { get; set; }

        public Cart? Cart { get; set; }

        public List<Order> Orders { get; set; } = new();
    }
}