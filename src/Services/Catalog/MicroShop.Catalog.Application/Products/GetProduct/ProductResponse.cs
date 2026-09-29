namespace MicroShop.Catalog.Application.Products.GetProduct;

public sealed record ProductResponse(
    Guid Id,
    string Name,
    decimal Price,
    DateTime CreatedAtUtc);
