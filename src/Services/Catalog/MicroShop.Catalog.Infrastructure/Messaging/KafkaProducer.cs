using System.Text.Json;
using Confluent.Kafka;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace MicroShop.Catalog.Infrastructure.Messaging;

internal sealed class KafkaProducer : IKafkaProducer, IDisposable
{
    private readonly IProducer<string, string> producer;
    private readonly ILogger<KafkaProducer> logger;

    public KafkaProducer(
        IOptions<KafkaOptions> options,
        ILogger<KafkaProducer> logger)
    {
        this.logger = logger;
        var configuration = new ProducerConfig
        {
            BootstrapServers = options.Value.BootstrapServers,
            Acks = Acks.All,
            EnableIdempotence = true
        };

        producer = new ProducerBuilder<string, string>(configuration).Build();
    }

    public async Task ProduceAsync<T>(
        string topic,
        string key,
        T message,
        CancellationToken cancellationToken)
    {
        var content = JsonSerializer.Serialize(message);

        var result = await producer.ProduceAsync(
            topic,
            new Message<string, string> { Key = key, Value = content },
            cancellationToken);

        logger.LogInformation(
            "Published Kafka message with key {Key} to {Topic} partition {Partition} offset {Offset}",
            key,
            result.Topic,
            result.Partition.Value,
            result.Offset.Value);
    }

    public void Dispose()
    {
        producer.Flush(TimeSpan.FromSeconds(5));
        producer.Dispose();
    }
}
