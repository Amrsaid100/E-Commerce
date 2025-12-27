using E_Commerce.DTOs.Auth;
using FluentValidation;

public class PromoteUserDtoValidator : AbstractValidator<PromoteUserDto>
{
    public PromoteUserDtoValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("User email is required")
            .EmailAddress().WithMessage("Invalid email format");
    }
}
