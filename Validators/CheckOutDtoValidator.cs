using E_Commerce.Dtos.UserDto;
using FluentValidation;

public class CheckOutDtoValidator : AbstractValidator<CheckOutDto>
{
    public CheckOutDtoValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email is required")
            .EmailAddress().WithMessage("Invalid email format");

        RuleFor(x => x.FullName)
            .NotEmpty().WithMessage("Full name is required")
            .Length(5, 70).WithMessage("Full name must be between 5 and 70 characters");

        RuleFor(x => x.PhoneNumber)
            .NotEmpty().WithMessage("Phone number is required")
            .Matches(@"^(\+?\d{10,15})$").WithMessage("Phone number is invalid — example: +201234567890");

        RuleFor(x => x.City)
            .NotEmpty().WithMessage("City is required")
            .Length(2, 50).WithMessage("City must be between 2 and 50 characters");

        RuleFor(x => x.Street)
            .NotEmpty().WithMessage("Street is required")
            .Length(2, 80).WithMessage("Street must be between 2 and 80 characters");

        RuleFor(x => x.Building)
            .NotEmpty().WithMessage("Building number is required")
            .Matches(@"^\d{1,5}$").WithMessage("Building number must be 1 to 5 digits");

        RuleFor(x => x.Appartment)
            .NotEmpty().WithMessage("Apartment number is required")
            .Matches(@"^\d{1,5}$").WithMessage("Apartment number must be 1 to 5 digits");
    }
}
