using System.ComponentModel.DataAnnotations;

namespace E_Commerce.DTOs.Auth
{
    public class VerifyOtpDto
    {
        [Required, EmailAddress]
        public string Email { get; set; }

        [Required]
        public string Otp { get; set; }
    }
}
