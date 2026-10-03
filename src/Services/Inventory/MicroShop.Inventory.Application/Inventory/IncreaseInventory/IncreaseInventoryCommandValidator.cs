using FluentValidation;

namespace MicroShop.Inventory.Application.Inventory.IncreaseInventory;

internal sealed class IncreaseInventoryCommandValidator
    : AbstractValidator<IncreaseInventoryCommand>
{
    public IncreaseInventoryCommandValidator()
    {
        RuleFor(command => command.ProductId)
            .NotEmpty();

        RuleFor(command => command.Quantity)
            .GreaterThan(0);
    }
}
