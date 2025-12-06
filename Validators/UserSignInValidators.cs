using FluentValidation;
using E_Commerce.Dtos.UserDto;

public class UserSignInValidator : AbstractValidator<UserSignInDto>
{
    public UserSignInValidator()
    {
        RuleFor(x => x.EmailAddress)
            .NotEmpty().WithMessage("Email address is required")
            .EmailAddress().WithMessage("Invalid email format");
    }
}
