using MicroShop.BuildingBlocks.Domain;
using MicroShop.Catalog.Application.Abstractions.Data;
using MicroShop.Catalog.Domain.Products;

namespace MicroShop.Catalog.Application.Products.GetProduct;

internal sealed class GetProductQueryHandler(IProductRepository productRepository)
    : IQueryHandler<GetProductQuery, ProductResponse>
{
    public async Task<Result<ProductResponse>> Handle(
        GetProductQuery query,
        CancellationToken cancellationToken)
    {
        var product = await productRepository.GetByIdAsync(
            query.ProductId,
            cancellationToken);

        if (product is null)
        {
            return ProductErrors.NotFound(query.ProductId);
        }

        return new ProductResponse(
            product.Id,
            product.Name,
            product.Price,
            product.CreatedAtUtc);
    }
}
