using MicroShop.Contracts;

namespace MicroShop.Catalog.Application.Abstractions.Messaging;

public interface IOutboxWriter
{
    void Add(ProductCreatedIntegrationEvent integrationEvent);
}
