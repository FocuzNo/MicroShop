using MicroShop.BuildingBlocks.Domain;

namespace MicroShop.BuildingBlocks.Infrastructure;

public sealed class DateTimeProvider : IDateTimeProvider
{
    public DateTime UtcNow => DateTime.UtcNow;
}
