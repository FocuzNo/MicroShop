using MicroShop.BuildingBlocks.Domain;
using MicroShop.Catalog.Application.Abstractions.Data;
using MicroShop.Catalog.Application.Abstractions.Messaging;
using MicroShop.Catalog.Domain.Products;
using MicroShop.Contracts;

namespace MicroShop.Catalog.Application.Products.CreateProduct;

internal sealed class CreateProductCommandHandler(
    IProductRepository productRepository,
    IOutboxWriter outboxWriter,
    IDateTimeProvider dateTimeProvider,
    IUnitOfWork unitOfWork)
    : ICommandHandler<CreateProductCommand, Guid>
{
    public async Task<Result<Guid>> Handle(
        CreateProductCommand command,
        CancellationToken cancellationToken)
    {
        var productResult = Product.Create(
            command.Name,
            command.Price,
            dateTimeProvider.UtcNow);

        if (productResult.IsFailure)
        {
            return productResult.Error;
        }

        var product = productResult.Value;

        var integrationEvent = new ProductCreatedIntegrationEvent(
            Guid.NewGuid(),
            dateTimeProvider.UtcNow,
            product.Id);

        productRepository.Add(product);
        outboxWriter.Add(integrationEvent);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return product.Id;
    }
}
