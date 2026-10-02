namespace MicroShop.Catalog.Application.Products.UpdateProduct;

public sealed record UpdateProductCommand(
    Guid ProductId,
    string Name,
    decimal Price)
    : ICommand;
