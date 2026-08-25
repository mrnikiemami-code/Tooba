using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Tooba.Persistence;
using Tooba.Wishlist.Domain;

namespace Tooba.Wishlist.Infrastructure.Persistence;

/// <summary>DbContext مالک schema مستقل wishlist و Outbox همان ماژول.</summary>
public sealed class WishlistDbContext : DbContext
{
    /// <summary>نام schema اختصاصی Wishlist.</summary>
    public const string Schema = "wishlist";
    /// <summary>DbContext را با گزینه‌های ماژول می‌سازد.</summary>
    public WishlistDbContext(DbContextOptions<WishlistDbContext> options) : base(options) { }
    /// <summary>مراجع خصوصی محصولات ذخیره‌شده.</summary>
    public DbSet<WishlistItem> Items => Set<WishlistItem>();
    /// <summary>پیام‌های Outbox ماژول.</summary>
    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();

    /// <inheritdoc />
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema(Schema);
        modelBuilder.Entity<WishlistItem>(entity =>
        {
            entity.ToTable("wishlist_items");
            entity.HasKey(x => x.WishlistItemId);
            entity.Property(x => x.WishlistItemId).ValueGeneratedNever();
            entity.HasIndex(x => new { x.OwnerUserId, x.ProductId }).IsUnique();
            entity.HasIndex(x => new { x.OwnerUserId, x.CreatedAt });
        });
        OutboxMessageMapping.Map(modelBuilder, Schema);
    }
}

/// <summary>کارخانهٔ design-time مهاجرت‌های Wishlist.</summary>
public sealed class WishlistDbContextFactory : IDesignTimeDbContextFactory<WishlistDbContext>
{
    /// <inheritdoc />
    public WishlistDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<WishlistDbContext>();
        ToobaNpgsql.ConfigureModuleContext(options, ToobaNpgsql.DesignTimeConnectionString(), WishlistDbContext.Schema, typeof(WishlistDbContext));
        return new WishlistDbContext(options.Options);
    }
}
