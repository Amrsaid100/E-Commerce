using System.ComponentModel.DataAnnotations;

namespace E_Commerce.Dtos.UserDto
{
    public class CheckOutDto
    {
        [EmailAddress,Required]
        public string Email {  get; set; }
        [Required]
        public string FullName { get; set; }
        [Required]
        public string PhoneNumber { get; set; }
        [Required]
        public string City { get; set; }
        [Required]
        public string Street { get; set; }
        [Required]
        public string Building { get; set; }
        [Required]
        public string Apartment { get; set; }

    }
}
