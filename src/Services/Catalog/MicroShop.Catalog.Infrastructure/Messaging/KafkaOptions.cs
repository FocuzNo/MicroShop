namespace MicroShop.Catalog.Infrastructure.Messaging;

public sealed class KafkaOptions
{
    public const string SectionName = "Kafka";
    public string BootstrapServers { get; init; } = string.Empty;
    public string ProductCreatedTopic { get; init; } = string.Empty;
}
