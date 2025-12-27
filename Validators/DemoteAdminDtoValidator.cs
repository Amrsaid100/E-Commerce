using E_Commerce.DTOs.Auth;
using FluentValidation;

public class DemoteAdminDtoValidator : AbstractValidator<DemoteAdminDto>
{
    public DemoteAdminDtoValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Admin email is required")
            .EmailAddress().WithMessage("Invalid email format");
    }
}
