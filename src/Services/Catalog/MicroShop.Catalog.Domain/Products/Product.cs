using MicroShop.BuildingBlocks.Domain;

namespace MicroShop.Catalog.Domain.Products;

public sealed class Product : Entity
{
    private Product()
    {
    }

    public string Name { get; private set; } = string.Empty;

    public decimal Price { get; private set; }

    public DateTime CreatedAtUtc { get; private set; }

    public static Result<Product> Create(
        string name,
        decimal price,
        DateTime createdAtUtc)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return ProductErrors.NameRequired;
        }

        var normalizedName = name.Trim();

        if (normalizedName.Length > 200)
        {
            return ProductErrors.NameTooLong;
        }

        if (price < 0)
        {
            return ProductErrors.InvalidPrice;
        }

        var product = new Product
        {
            Id = Guid.NewGuid(),
            Name = normalizedName,
            Price = price,
            CreatedAtUtc = createdAtUtc
        };

        return product;
    }
}
