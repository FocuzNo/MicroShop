using MicroShop.BuildingBlocks.Domain;
using MicroShop.Catalog.Application.Abstractions.Data;
using MicroShop.Catalog.Domain.Products;

namespace MicroShop.Catalog.Application.Products.UpdateProduct;

internal sealed class UpdateProductCommandHandler(
    IProductRepository productRepository,
    IUnitOfWork unitOfWork)
    : ICommandHandler<UpdateProductCommand>
{
    public async Task<Result> Handle(
        UpdateProductCommand command,
        CancellationToken cancellationToken)
    {
        var product = await productRepository.GetByIdAsync(
            command.ProductId,
            cancellationToken);

        if (product is null)
        {
            return Result.Failure(ProductErrors.NotFound(command.ProductId));
        }

        var updateResult = product.Update(
            command.Name,
            command.Price);

        if (updateResult.IsFailure)
        {
            return Result.Failure(updateResult.Error);
        }

        productRepository.Update(product);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
