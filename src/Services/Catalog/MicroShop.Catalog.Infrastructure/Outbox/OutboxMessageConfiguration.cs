using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MicroShop.Catalog.Infrastructure.Outbox;

internal sealed class OutboxMessageConfiguration : IEntityTypeConfiguration<OutboxMessage>
{
    public void Configure(EntityTypeBuilder<OutboxMessage> builder)
    {
        builder.ToTable("outbox_messages");
        builder.HasKey(message => message.Id);
        builder.Property(message => message.Type).HasMaxLength(500).IsRequired();
        builder.Property(message => message.Content).HasColumnType("jsonb").IsRequired();
        builder.Property(message => message.Error).HasMaxLength(2000);
        builder.HasIndex(message => message.ProcessedAtUtc);
    }
}
