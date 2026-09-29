using MediatR;
using MicroShop.BuildingBlocks.Domain;

namespace MicroShop.BuildingBlocks.Application;

public interface ICommand : IRequest<Result>;

public interface ICommand<TResponse> : IRequest<Result<TResponse>>;
