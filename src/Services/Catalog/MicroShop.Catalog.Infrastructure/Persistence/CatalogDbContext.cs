using Microsoft.EntityFrameworkCore;
using MicroShop.Catalog.Application.Abstractions.Data;
using MicroShop.Catalog.Domain.Products;
using MicroShop.Catalog.Infrastructure.Outbox;

namespace MicroShop.Catalog.Infrastructure.Persistence;

public sealed class CatalogDbContext(DbContextOptions<CatalogDbContext> options)
    : DbContext(options), IUnitOfWork
{
    public DbSet<Product> Products => Set<Product>();

    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(CatalogDbContext).Assembly);
    }
}
