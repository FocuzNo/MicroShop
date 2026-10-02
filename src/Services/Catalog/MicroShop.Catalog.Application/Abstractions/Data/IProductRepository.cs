using MicroShop.Catalog.Domain.Products;

namespace MicroShop.Catalog.Application.Abstractions.Data;

public interface IProductRepository
{
    Task<Product?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken);

    void Add(Product product);

    void Update(Product product);
}
