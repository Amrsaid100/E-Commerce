using E_Commerce.Dtos.OrderDto;
using E_Commerce.Dtos.UserDto;
using FluentValidation;
using System;
using System.Linq;

public class UserOrderValidator : AbstractValidator<UserOrderDto>
{
    public UserOrderValidator(IValidator<OrderItemDto> orderItemValidator)
    {
        // Ensure items collection exists and contains at least one non-null item
        RuleFor(x => x.Items)
            .NotNull().WithMessage("Order items must be provided")
            .NotEmpty().WithMessage("Order must contain at least one item")
            .Must(items => items.All(i => i != null))
            .WithMessage("Items must not contain null entries");

        // Validate each order item using DI
        RuleForEach(x => x.Items)
            .SetValidator(orderItemValidator);

        // Validate total price (with rounding tolerance)
        RuleFor(x => x.TotalPrice)
            .GreaterThanOrEqualTo(0)
            .WithMessage("Total price must be 0 or more")
            .Must((dto, total) =>
            {
                var expected = dto.Items.Sum(i => i.UnitPrice * i.Quantity);
                return Math.Abs(total - expected) <= 0.01m;
            })
            .WithMessage("Total price must equal the sum of all item prices")
            .When(x => x.Items != null && x.Items.Any());
    }
}
