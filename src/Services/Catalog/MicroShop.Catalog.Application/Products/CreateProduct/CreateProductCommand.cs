namespace MicroShop.Catalog.Application.Products.CreateProduct;

public sealed record CreateProductCommand(
    string Name,
    decimal Price)
    : ICommand<Guid>;
