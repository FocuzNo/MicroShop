using Microsoft.AspNetCore.Routing;

namespace MicroShop.BuildingBlocks.Infrastructure;

public interface IEndpoint
{
    void MapEndpoint(IEndpointRouteBuilder app);
}
