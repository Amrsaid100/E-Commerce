using System.ComponentModel.DataAnnotations;

namespace E_Commerce.Dtos.UserDto
{
    public class  UserSignInDto
    {
        [Required,EmailAddress]
        public string EmailAddress { get; set; }
    }
}
