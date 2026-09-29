using MicroShop.BuildingBlocks.Domain;

namespace MicroShop.Catalog.Domain.Products;

public static class ProductErrors
{
    public static readonly Error NameRequired = Error.Validation(
        "Product.NameRequired",
        "The product name is required.");

    public static readonly Error NameTooLong = Error.Validation(
        "Product.NameTooLong",
        "The product name must not exceed 200 characters.");

    public static readonly Error InvalidPrice = Error.Validation(
        "Product.InvalidPrice",
        "The product price cannot be negative.");

    public static Error NotFound(Guid productId) => Error.NotFound(
        "Product.NotFound",
        $"The product with identifier '{productId}' was not found.");
}
