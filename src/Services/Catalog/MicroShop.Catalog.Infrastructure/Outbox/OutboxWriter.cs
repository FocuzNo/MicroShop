using System.Text.Json;
using MicroShop.Catalog.Application.Abstractions.Messaging;
using MicroShop.Catalog.Infrastructure.Persistence;
using MicroShop.Contracts;

namespace MicroShop.Catalog.Infrastructure.Outbox;

internal sealed class OutboxWriter(CatalogDbContext dbContext) : IOutboxWriter
{
    public void Add(ProductCreatedIntegrationEvent integrationEvent)
    {
        var content = JsonSerializer.Serialize(integrationEvent);

        var outboxMessage = new OutboxMessage(
            integrationEvent.Id,
            typeof(ProductCreatedIntegrationEvent).FullName!,
            content,
            integrationEvent.OccurredAtUtc);

        dbContext.OutboxMessages.Add(outboxMessage);
    }
}
