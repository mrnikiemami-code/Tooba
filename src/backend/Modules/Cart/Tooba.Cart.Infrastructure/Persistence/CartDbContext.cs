using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Tooba.Cart.Domain;
using Tooba.Persistence;

namespace Tooba.Cart.Infrastructure.Persistence;

/// <summary>
/// DbContext مالک schema <c>cart</c>. سفارش و پرداخت و موجودی را نگه نمی‌دارد.
/// </summary>
public sealed class CartDbContext : DbContext
{
    /// <summary>
    /// schema اختصاصی Cart.
    /// </summary>
    public const string Schema = "cart";

    /// <summary>
    /// DbContext را با گزینه‌های Host می‌سازد.
    /// </summary>
    public CartDbContext(DbContextOptions<CartDbContext> options)
        : base(options)
    {
    }

    /// <summary>
    /// سبدهای پایدار.
    /// </summary>
    public DbSet<ShoppingCart> Carts => Set<ShoppingCart>();

    /// <summary>
    /// خطوط Offer داخل سبد.
    /// </summary>
    public DbSet<CartLine> Lines => Set<CartLine>();

    /// <summary>
    /// Outbox همین ماژول.
    /// </summary>
    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();

    /// <inheritdoc />
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema(Schema);
        modelBuilder.Entity<ShoppingCart>(entity =>
        {
            entity.ToTable("carts");
            entity.HasKey(x => x.CartId);
            entity.Property(x => x.CartId).ValueGeneratedNever();
            entity.Property(x => x.Status).HasConversion<string>().HasMaxLength(32);
            entity.Property(x => x.AccessKind).HasConversion<string>().HasMaxLength(32);
            entity.Property(x => x.GuestCredentialHash).HasMaxLength(128);
            entity.Property(x => x.Market).HasMaxLength(16);
            entity.Property(x => x.Currency).HasMaxLength(3);
            entity.Property(x => x.Channel).HasConversion<string>().HasMaxLength(32);
            entity.Property(x => x.ConversionIntent).HasConversion<string>().HasMaxLength(32);
            entity.Property(x => x.Version).IsConcurrencyToken();
            entity.Ignore(x => x.DomainEvents);
            entity.HasMany(x => x.Lines).WithOne().HasForeignKey(x => x.CartId).OnDelete(DeleteBehavior.Cascade);
            entity.HasIndex(x => x.OwnerUserId);
            entity.HasIndex(x => x.ExpiresAt);
        });
        modelBuilder.Entity<CartLine>(entity =>
        {
            entity.ToTable("cart_lines");
            entity.HasKey(x => x.LineId);
            entity.Property(x => x.LineId).ValueGeneratedNever();
            entity.Property(x => x.QuotedAmount).HasPrecision(19, 4);
            entity.Property(x => x.QuotedCurrency).HasMaxLength(3);
            entity.HasIndex(x => new { x.CartId, x.OfferId }).IsUnique();
        });
        OutboxMessageMapping.Map(modelBuilder, Schema);
    }
}

/// <summary>
/// کارخانهٔ design-time مهاجرت Cart.
/// </summary>
public sealed class CartDbContextFactory : IDesignTimeDbContextFactory<CartDbContext>
{
    /// <inheritdoc />
    public CartDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<CartDbContext>();
        ToobaNpgsql.ConfigureModuleContext(
            options,
            ToobaNpgsql.DesignTimeConnectionString(),
            CartDbContext.Schema,
            typeof(CartDbContext));
        return new CartDbContext(options.Options);
    }
}
