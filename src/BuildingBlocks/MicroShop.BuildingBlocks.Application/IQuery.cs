using MediatR;
using MicroShop.BuildingBlocks.Domain;

namespace MicroShop.BuildingBlocks.Application;

public interface IQuery<TResponse> : IRequest<Result<TResponse>>;
