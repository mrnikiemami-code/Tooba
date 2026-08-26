using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Tooba.Inventory.Domain;
using Tooba.Persistence;

namespace Tooba.Inventory.Infrastructure.Persistence;

/// <summary>
/// DbContext مالک schema <c>inventory</c>. موجودی را روی Product یا Offer نگه نمی‌دارد.
/// </summary>
public sealed class InventoryDbContext : DbContext
{
    /// <summary>
    /// schema اختصاصی Inventory.
    /// </summary>
    public const string Schema = "inventory";

    /// <summary>
    /// DbContext را با گزینه‌های Host می‌سازد.
    /// </summary>
    public InventoryDbContext(DbContextOptions<InventoryDbContext> options)
        : base(options)
    {
    }

    /// <summary>
    /// محل‌های نگهداری.
    /// </summary>
    public DbSet<InventoryLocation> Locations => Set<InventoryLocation>();

    /// <summary>
    /// موقعیت‌های موجودی Offer در محل.
    /// </summary>
    public DbSet<StockPosition> Positions => Set<StockPosition>();

    /// <summary>
    /// رزروهای Held/Released/Consumed.
    /// </summary>
    public DbSet<StockReservation> Reservations => Set<StockReservation>();

    /// <summary>
    /// dedup restock مرجوعی.
    /// </summary>
    public DbSet<ReturnRestockInboxRecord> ReturnRestockInbox => Set<ReturnRestockInboxRecord>();

    /// <summary>
    /// Outbox همین ماژول.
    /// </summary>
    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();

    /// <inheritdoc />
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema(Schema);
        modelBuilder.Entity<InventoryLocation>(entity =>
        {
            entity.ToTable("locations");
            entity.HasKey(x => x.LocationId);
            entity.Property(x => x.LocationId).ValueGeneratedNever();
            entity.Property(x => x.Code).HasMaxLength(32);
            entity.Property(x => x.Name).HasMaxLength(128);
            entity.Property(x => x.Status).HasConversion<string>().HasMaxLength(32);
            entity.Ignore(x => x.DomainEvents);
            entity.HasIndex(x => x.Code).IsUnique();
        });
        modelBuilder.Entity<StockPosition>(entity =>
        {
            entity.ToTable("stock_positions");
            entity.HasKey(x => x.StockItemId);
            entity.Property(x => x.StockItemId).ValueGeneratedNever();
            entity.Ignore(x => x.Available);
            entity.Ignore(x => x.DomainEvents);
            entity.HasIndex(x => new { x.OfferId, x.LocationId }).IsUnique();
        });
        modelBuilder.Entity<StockReservation>(entity =>
        {
            entity.ToTable("reservations");
            entity.HasKey(x => x.ReservationId);
            entity.Property(x => x.ReservationId).ValueGeneratedNever();
            entity.Property(x => x.Status).HasConversion<string>().HasMaxLength(32);
            entity.Property(x => x.ExternalReference).HasMaxLength(128);
            entity.Property(x => x.IdempotencyKey).HasMaxLength(128);
            entity.HasIndex(x => x.IdempotencyKey).IsUnique().HasFilter("idempotency_key IS NOT NULL");
        });
        modelBuilder.Entity<ReturnRestockInboxRecord>(entity =>
        {
            entity.ToTable("return_restock_inbox");
            entity.HasKey(x => x.IdempotencyKey);
            entity.Property(x => x.IdempotencyKey).HasMaxLength(128);
            entity.HasIndex(x => x.ReservationId);
        });
        OutboxMessageMapping.Map(modelBuilder, Schema);
    }
}

/// <summary>
/// کارخانهٔ design-time مهاجرت Inventory.
/// </summary>
public sealed class InventoryDbContextFactory : IDesignTimeDbContextFactory<InventoryDbContext>
{
    /// <inheritdoc />
    public InventoryDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<InventoryDbContext>();
        ToobaNpgsql.ConfigureModuleContext(
            options,
            ToobaNpgsql.DesignTimeConnectionString(),
            InventoryDbContext.Schema,
            typeof(InventoryDbContext));
        return new InventoryDbContext(options.Options);
    }
}
