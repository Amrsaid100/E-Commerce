using System.ComponentModel.DataAnnotations;

namespace E_Commerce.DTOs.Auth
{
    public class RequestOtpDto
    {
        [Required, EmailAddress]
        public string Email { get; set; }
    }
}
