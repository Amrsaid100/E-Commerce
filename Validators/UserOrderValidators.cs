using FluentValidation;
using E_Commerce.Dtos.UserDto;

public class UserOrderValidator : AbstractValidator<UserOrderDto>
{
    public UserOrderValidator()
    {
        RuleFor(x => x.Items)
            .NotEmpty().WithMessage("Order must contain at least one item");

        RuleForEach(x => x.Items).SetValidator(new OrderItemValidator()); 

        RuleFor(x => x.TotalPrice)
            .GreaterThanOrEqualTo(0).WithMessage("Total price must be 0 or more")
            .Must((dto, total) => total == dto.Items.Sum(i => i.Price * i.Quantity))
            .WithMessage("Total price must equal the sum of all item prices");
    }
}
