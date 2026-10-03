using FluentValidation;

namespace MicroShop.Inventory.Application.Inventory.DecreaseInventory;

internal sealed class DecreaseInventoryCommandValidator
    : AbstractValidator<DecreaseInventoryCommand>
{
    public DecreaseInventoryCommandValidator()
    {
        RuleFor(command => command.ProductId)
            .NotEmpty();

        RuleFor(command => command.Quantity)
            .GreaterThan(0);
    }
}
