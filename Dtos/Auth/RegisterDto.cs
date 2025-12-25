using System.ComponentModel.DataAnnotations;

namespace E_Commerce.DTOs.Auth
{
    public class RegisterDto
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; }
        [Required]
        [MinLength(6)]
        public string Password { get; set; }
        public string Name { get; set; }
    }
}