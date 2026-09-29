namespace MicroShop.Contracts;

public sealed record ProductCreatedIntegrationEvent(
    Guid Id,
    DateTime OccurredAtUtc,
    Guid ProductId);
