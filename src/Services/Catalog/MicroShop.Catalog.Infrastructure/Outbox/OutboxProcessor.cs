using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MicroShop.BuildingBlocks.Domain;
using MicroShop.Catalog.Infrastructure.Messaging;
using MicroShop.Catalog.Infrastructure.Persistence;
using MicroShop.Contracts;

namespace MicroShop.Catalog.Infrastructure.Outbox;

internal sealed class OutboxProcessor(
    IServiceScopeFactory scopeFactory,
    IOptions<KafkaOptions> options,
    IDateTimeProvider dateTimeProvider,
    ILogger<OutboxProcessor> logger)
    : BackgroundService
{
    private readonly KafkaOptions kafkaOptions = options.Value;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(2));

        do
        {
            await ProcessBatchAsync(stoppingToken);
        }
        while (await timer.WaitForNextTickAsync(stoppingToken));
    }

    private async Task ProcessBatchAsync(CancellationToken cancellationToken)
    {
        using var scope = scopeFactory.CreateScope();

        var dbContext = scope.ServiceProvider.GetRequiredService<CatalogDbContext>();
        var kafkaProducer = scope.ServiceProvider.GetRequiredService<IKafkaProducer>();

        var messages = await dbContext.OutboxMessages
            .Where(message => message.ProcessedAtUtc == null)
            .OrderBy(message => message.OccurredAtUtc)
            .Take(20)
            .ToListAsync(cancellationToken);

        foreach (var message in messages)
        {
            try
            {
                await PublishAsync(
                    message,
                    kafkaProducer,
                    cancellationToken);

                message.MarkProcessed(dateTimeProvider.UtcNow);

                await dbContext.SaveChangesAsync(cancellationToken);

                logger.LogInformation(
                    "Processed Outbox message {OutboxMessageId}",
                    message.Id);
            }
            catch (Exception exception) when (exception is not OperationCanceledException)
            {
                message.MarkFailed(exception.Message);

                await dbContext.SaveChangesAsync(cancellationToken);

                logger.LogError(
                    exception,
                    "Failed to publish Outbox message {OutboxMessageId}",
                    message.Id);
            }
        }
    }

    private async Task PublishAsync(
        OutboxMessage message,
        IKafkaProducer kafkaProducer,
        CancellationToken cancellationToken)
    {
        if (message.Type != typeof(ProductCreatedIntegrationEvent).FullName)
        {
            throw new InvalidOperationException($"Unsupported Outbox message type '{message.Type}'.");
        }

        var integrationEvent = JsonSerializer.Deserialize<ProductCreatedIntegrationEvent>(message.Content)
            ?? throw new InvalidOperationException("The ProductCreatedIntegrationEvent payload is invalid.");

        await kafkaProducer.ProduceAsync(
            kafkaOptions.ProductCreatedTopic,
            integrationEvent.ProductId.ToString(),
            integrationEvent,
            cancellationToken);
    }
}
