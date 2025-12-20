using E_Commerce.Dtos.OrderDto;
using FluentValidation;

public class OrderItemValidator : AbstractValidator<OrderItemDto>
{
    public OrderItemValidator()
    {
        RuleFor(x => x.ProductVariantId)
            .GreaterThan(0)
            .WithMessage("ProductVariantId must be a valid id");

        RuleFor(x => x.ProductName)
            .NotEmpty()
            .WithMessage("Product name is required")
            .Length(2, 100)
            .WithMessage("Product name must be between 2 and 100 characters");

        RuleFor(x => x.Quantity)
            .GreaterThan(0)
            .WithMessage("Quantity must be greater than zero")
            .LessThanOrEqualTo(100)
            .WithMessage("Quantity cannot exceed 100 per item");

        RuleFor(x => x.UnitPrice)
            .GreaterThan(0)
            .WithMessage("Unit price must be greater than zero")
            .LessThanOrEqualTo(100_000)
            .WithMessage("Unit price exceeds the allowed limit");
    }
}
