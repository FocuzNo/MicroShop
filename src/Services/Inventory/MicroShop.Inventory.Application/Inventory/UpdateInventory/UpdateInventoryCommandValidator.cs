using FluentValidation;

namespace MicroShop.Inventory.Application.Inventory.UpdateInventory;

internal sealed class UpdateInventoryCommandValidator : AbstractValidator<UpdateInventoryCommand>
{
    public UpdateInventoryCommandValidator()
    {
        RuleFor(command => command.ProductId).NotEmpty();
        RuleFor(command => command.Quantity).GreaterThanOrEqualTo(0);
    }
}
