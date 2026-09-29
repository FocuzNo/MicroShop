using MediatR;
using MicroShop.BuildingBlocks.Domain;

namespace MicroShop.BuildingBlocks.Application;

public interface IQueryHandler<in TQuery, TResponse> : IRequestHandler<TQuery, Result<TResponse>>
    where TQuery : IQuery<TResponse>;
