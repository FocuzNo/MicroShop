using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MicroShop.Inventory.Domain.Inventory;

namespace MicroShop.Inventory.Infrastructure.Persistence;

internal sealed class InventoryItemConfiguration : IEntityTypeConfiguration<InventoryItem>
{
    public void Configure(EntityTypeBuilder<InventoryItem> builder)
    {
        builder.ToTable("inventory_items");
        builder.HasKey(inventoryItem => inventoryItem.Id);
        builder.HasIndex(inventoryItem => inventoryItem.ProductId).IsUnique();
        builder.Property(inventoryItem => inventoryItem.ProductId).IsRequired();
        builder.Property(inventoryItem => inventoryItem.Quantity).IsRequired();
        builder.Property(inventoryItem => inventoryItem.CreatedAtUtc).IsRequired();
        builder.Property(inventoryItem => inventoryItem.UpdatedAtUtc).IsRequired();
    }
}
