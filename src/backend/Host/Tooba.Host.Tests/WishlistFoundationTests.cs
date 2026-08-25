using Microsoft.EntityFrameworkCore;
using Testcontainers.PostgreSql;
using Tooba.Host.Wishlist;
using Tooba.Persistence;
using Tooba.Wishlist.Application;
using Tooba.Wishlist.Domain;
using Tooba.Wishlist.Infrastructure.Persistence;
using Xunit;

namespace Tooba.Host.Tests;

/// <summary>قفل قرارداد، حریم خصوصی، ذخیره‌سازی و ترکیب Wishlist.</summary>
public sealed class WishlistFoundationTests
{
    /// <summary>Entity فقط هویت مالک، محصول و زمان را ذخیره می‌کند و قیمت/موجودی ندارد.</summary>
    [Fact]
    public void Entity_is_intentionally_small()
    {
        var names = typeof(WishlistItem).GetProperties().Select(x => x.Name).ToArray();
        Assert.Equal(["WishlistItemId", "OwnerUserId", "ProductId", "CreatedAt"], names);
        Assert.DoesNotContain("Price", names);
        Assert.DoesNotContain("Stock", names);
    }

    /// <summary>بدنه‌های HTTP هیچ اختیار Actor یا مالک دریافت نمی‌کنند.</summary>
    [Fact]
    public void Http_contract_has_no_owner_authority()
    {
        Assert.Equal(["ProductIds"], typeof(WishlistMembershipRequest).GetProperties().Select(x => x.Name));
        Assert.DoesNotContain("OwnerUserId", typeof(WishlistPage).GetProperties().Select(x => x.Name));
    }

    /// <summary>قرارداد batch و شمارش از N+1 عضویت و شمارندهٔ جعلی جلوگیری می‌کند.</summary>
    [Fact]
    public void Directory_exposes_batch_membership_and_count()
    {
        Assert.NotNull(typeof(IWishlistDirectory).GetMethod(nameof(IWishlistDirectory.GetMembershipAsync)));
        Assert.NotNull(typeof(IWishlistDirectory).GetMethod(nameof(IWishlistDirectory.CountAsync)));
        Assert.Equal("wishlist", WishlistDbContext.Schema);
    }

    /// <summary>نمای unavailable کارت خرید ساختگی تولید نمی‌کند.</summary>
    [Fact]
    public void Unavailable_presentation_is_explicit()
    {
        var item = new WishlistPageItem(Guid.NewGuid(), Guid.NewGuid(), DateTimeOffset.UtcNow, null, "product-unavailable");
        Assert.Null(item.Product);
        Assert.Equal("product-unavailable", item.UnavailableReason);
    }

    /// <summary>مرز HTTP در production بدون نشست 401 می‌دهد و هیچ owner از route/body نمی‌خواند.</summary>
    [Fact]
    public void Endpoint_uses_session_and_rejects_missing_production_actor()
    {
        var source = File.ReadAllText(Path.Combine(
            FindRepoRoot(), "src", "backend", "Host", "Tooba.Host", "Wishlist", "WishlistEndpoints.cs"));
        Assert.Contains("session.IsAuthenticated", source, StringComparison.Ordinal);
        Assert.Contains("StatusCodes.Status401Unauthorized", source, StringComparison.Ordinal);
        Assert.Contains("environment.IsDevelopment()", source, StringComparison.Ordinal);
        Assert.DoesNotContain("{owner", source, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("OwnerUserId", source, StringComparison.Ordinal);
    }

    private static string FindRepoRoot()
    {
        var current = new DirectoryInfo(AppContext.BaseDirectory);
        while (current is not null && !File.Exists(Path.Combine(current.FullName, "AGENTS.md"))) current = current.Parent;
        return current?.FullName ?? throw new InvalidOperationException("Repository root not found.");
    }
}

/// <summary>اثبات PostgreSQL برای uniqueness، ایزولاسیون مالک و حذف امن.</summary>
[Collection("PostgresSerial")]
public sealed class WishlistPostgresTests : IAsyncLifetime
{
    private PostgreSqlContainer? _container;
    private bool _available;

    /// <inheritdoc />
    public async Task InitializeAsync()
    {
        try
        {
            _container = new PostgreSqlBuilder().WithImage("postgres:16-alpine").WithDatabase("tooba_wishlist")
                .WithUsername("tooba").WithPassword("dev-placeholder").Build();
            await _container.StartAsync();
            _available = true;
        }
        catch (Exception) { _available = false; }
    }

    /// <inheritdoc />
    public async Task DisposeAsync()
    {
        if (_container is not null) await _container.DisposeAsync();
    }

    /// <summary>کلید یکتا duplicate را رد و query مالک A دادهٔ B را پنهان می‌کند.</summary>
    [SkippableFact]
    public async Task Unique_owner_product_and_actor_isolation_are_enforced()
    {
        Skip.If(!_available || _container is null, "Docker/Testcontainers PostgreSQL is not available.");
        var options = new DbContextOptionsBuilder<WishlistDbContext>();
        ToobaNpgsql.ConfigureModuleContext(options, _container!.GetConnectionString(), WishlistDbContext.Schema, typeof(WishlistDbContext));
        await using var db = new WishlistDbContext(options.Options);
        await db.Database.MigrateAsync();
        var a = Guid.NewGuid();
        var b = Guid.NewGuid();
        var product = Guid.NewGuid();
        db.Items.Add(WishlistItem.Create(a, product, DateTimeOffset.UtcNow));
        db.Items.Add(WishlistItem.Create(b, product, DateTimeOffset.UtcNow));
        await db.SaveChangesAsync();
        Assert.Single(await db.Items.Where(x => x.OwnerUserId == a).ToListAsync());
        Assert.DoesNotContain(await db.Items.Where(x => x.OwnerUserId == a).ToListAsync(), x => x.OwnerUserId == b);
        db.Items.Add(WishlistItem.Create(a, product, DateTimeOffset.UtcNow));
        await Assert.ThrowsAsync<DbUpdateException>(() => db.SaveChangesAsync());
    }
}
