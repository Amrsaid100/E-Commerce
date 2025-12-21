using E_Commerce.Dtos.UserDto;
using FluentValidation;
using System.Linq;

public class UserProductVariantValidator : AbstractValidator<UserProductVariantDto>
{
    //  static list
    private static readonly List<string> AllowedSizes = new List<string> { "S", "M", "L", "XL", "XXL","XXXL" };

    public UserProductVariantValidator()
    {
        RuleFor(x => x.ProductVariantId)
            .GreaterThan(0).WithMessage("ProductVariantId must be greater than 0");

        RuleFor(x => x.Description)
            .NotEmpty().WithMessage("Description is required")
            .MaximumLength(500).WithMessage("Description must not exceed 500 characters");

        RuleFor(x => x.Price)
            .GreaterThanOrEqualTo(0).WithMessage("Price must be 0 or more");

        RuleFor(x => x.Image)
            .MaximumLength(250).WithMessage("Image URL must not exceed 250 characters")
            .When(x => !string.IsNullOrEmpty(x.Image));

        //Use The Static List
        RuleFor(x => x.size)
            .Must(size => AllowedSizes.Contains(size))
            .WithMessage("Size must be one of: S, M, L, XL, XXL,XXXL");

        RuleFor(x => x.quantity)
            .GreaterThanOrEqualTo(0).WithMessage("Quantity must be 0 or more");
    }
}
