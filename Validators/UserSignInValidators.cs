using E_Commerce.Dtos.UserDto;
using FluentValidation;

public class UserSignInValidator : AbstractValidator<UserSignInDto>
{
    public UserSignInValidator()
    {
        RuleFor(x => x.EmailAddress)
            .NotEmpty().WithMessage("Email address is required")
            .EmailAddress().WithMessage("Invalid email format");

        // Remove password validation completely because we're using OTP
        // RuleFor(x => x.Password) ...  <-- removed
    }
}
