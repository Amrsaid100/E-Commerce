using E_Commerce.DTOs.Auth;
using FluentValidation;

public class VerifyOtpDtoValidator : AbstractValidator<VerifyOtpDto>
{
    public VerifyOtpDtoValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email is required")
            .EmailAddress().WithMessage("Invalid email format");

        RuleFor(x => x.Otp)
            .NotEmpty().WithMessage("OTP is required")
            .Matches(@"^\d{6}$").WithMessage("OTP must be a 6-digit number");
    }
}
