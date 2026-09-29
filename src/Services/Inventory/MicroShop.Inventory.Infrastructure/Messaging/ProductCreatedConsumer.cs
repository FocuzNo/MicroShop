using System.Text.Json;
using Confluent.Kafka;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MicroShop.BuildingBlocks.Domain;
using MicroShop.Contracts;
using MicroShop.Inventory.Domain.Inventory;
using MicroShop.Inventory.Infrastructure.Inbox;
using MicroShop.Inventory.Infrastructure.Persistence;

namespace MicroShop.Inventory.Infrastructure.Messaging;

internal sealed class ProductCreatedConsumer(
    IServiceScopeFactory scopeFactory,
    IOptions<KafkaOptions> options,
    ILogger<ProductCreatedConsumer> logger)
    : BackgroundService
{
    private readonly KafkaOptions kafkaOptions = options.Value;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var configuration = new ConsumerConfig
        {
            BootstrapServers = kafkaOptions.BootstrapServers,
            GroupId = kafkaOptions.ConsumerGroup,
            EnableAutoCommit = false,
            AutoOffsetReset = AutoOffsetReset.Earliest
        };

        using var consumer = new ConsumerBuilder<string, string>(configuration).Build();

        consumer.Subscribe(kafkaOptions.ProductCreatedTopic);

        try
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                ConsumeResult<string, string> consumeResult;
                try
                {
                    consumeResult = consumer.Consume(stoppingToken);
                }
                catch (ConsumeException exception)
                {
                    logger.LogError(
                        exception,
                        "Kafka consume failed for topic {Topic}",
                        kafkaOptions.ProductCreatedTopic);

                    await Task.Delay(
                        TimeSpan.FromSeconds(2),
                        stoppingToken);

                    continue;
                }

                try
                {
                    await ProcessAsync(
                        consumeResult,
                        stoppingToken);

                    consumer.Commit(consumeResult);
                }
                catch (Exception exception) when (exception is not OperationCanceledException)
                {
                    logger.LogError(
                        exception,
                        "Kafka processing failed for {Topic} partition {Partition} offset {Offset}; offset was not committed",
                        consumeResult.Topic,
                        consumeResult.Partition.Value,
                        consumeResult.Offset.Value);

                    consumer.Seek(consumeResult.TopicPartitionOffset);

                    await Task.Delay(
                        TimeSpan.FromSeconds(2),
                        stoppingToken);
                }
            }
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            logger.LogInformation("Kafka consumer is stopping");
        }
        finally
        {
            consumer.Close();
        }
    }

    private async Task ProcessAsync(
        ConsumeResult<string, string> consumeResult,
        CancellationToken cancellationToken)
    {
        var integrationEvent = JsonSerializer.Deserialize<ProductCreatedIntegrationEvent>(consumeResult.Message.Value)
            ?? throw new InvalidOperationException("The ProductCreatedIntegrationEvent payload is invalid.");

        logger.LogInformation(
            "Consumed ProductCreatedIntegrationEvent {EventId} for product {ProductId} with key {Key} from {Topic} partition {Partition} offset {Offset}",
            integrationEvent.Id,
            integrationEvent.ProductId,
            consumeResult.Message.Key,
            consumeResult.Topic,
            consumeResult.Partition.Value,
            consumeResult.Offset.Value);

        using var scope = scopeFactory.CreateScope();

        var dbContext = scope.ServiceProvider.GetRequiredService<InventoryDbContext>();
        var alreadyProcessed = await dbContext.InboxMessages
            .AsNoTracking()
            .AnyAsync(
                message => message.Id == integrationEvent.Id,
                cancellationToken);

        if (alreadyProcessed)
        {
            logger.LogInformation(
                "Ignored duplicate Inbox message {EventId} for product {ProductId}",
                integrationEvent.Id,
                integrationEvent.ProductId);

            return;
        }

        var dateTimeProvider = scope.ServiceProvider.GetRequiredService<IDateTimeProvider>();
        var nowUtc = dateTimeProvider.UtcNow;

        var inventoryResult = InventoryItem.Create(
            integrationEvent.ProductId,
            nowUtc);

        if (inventoryResult.IsFailure)
        {
            throw new InvalidOperationException(inventoryResult.Error.Description);
        }

        var inboxMessage = new InboxMessage(
            integrationEvent.Id,
            typeof(ProductCreatedIntegrationEvent).FullName!,
            nowUtc,
            nowUtc);

        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);

        dbContext.InboxMessages.Add(inboxMessage);
        dbContext.InventoryItems.Add(inventoryResult.Value);

        await dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        logger.LogInformation(
            "Created InventoryItem {InventoryItemId} for product {ProductId} from Inbox message {EventId}",
            inventoryResult.Value.Id,
            integrationEvent.ProductId,
            integrationEvent.Id);
    }
}
