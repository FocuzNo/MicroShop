namespace MicroShop.BuildingBlocks.Domain;

public interface IDateTimeProvider
{
    DateTime UtcNow { get; }
}
