using E_Commerce.DTOs.Auth;
using FluentValidation;

public class RequestOtpDtoValidator : AbstractValidator<RequestOtpDto>
{
    public RequestOtpDtoValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email is required")
            .EmailAddress().WithMessage("Invalid email format");
    }
}
