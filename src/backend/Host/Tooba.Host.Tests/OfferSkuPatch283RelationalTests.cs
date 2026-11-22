using Microsoft.EntityFrameworkCore;
using Npgsql;
using Tooba.BuildingBlocks;
using Tooba.Offer.Domain.Aggregates;
using Tooba.Offer.Domain.ValueObjects;
using Tooba.Offer.Infrastructure.Persistence;
using Tooba.Persistence;
using Xunit;

namespace Tooba.Host.Tests;

[Collection("PostgresSerial")]
public sealed class OfferSkuPatch283RelationalTests
{
    private static readonly Lazy<(bool Ok, string Reason)> Target = new(ValidateTarget);
    private static string Connection =>
        Environment.GetEnvironmentVariable("TB283_CONNECTION") is { Length: > 0 } raw
            ? raw.Trim()
            : "Host=127.0.0.1;Port=5432;Username=admin;Password=123456;Database=tooba_alpha";

    [SkippableFact]
    public async Task Draft_sku_and_cleared_limits_persist_without_status_transition()
    {
        Skip.IfNot(Target.Value.Ok, Target.Value.Reason);
        await using var db = CreateDb();
        await using var tx = await db.Database.BeginTransactionAsync();
        var now = DateTimeOffset.UtcNow;
        var offer = SellerOffer.Create(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Tooba.Offer.Domain.ValueObjects.SalesChannel.Marketplace, "SKU-1", now);
        var limits = offer.SetOrderQuantityLimits(2, 5, now);
        Assert.True(limits.IsSuccess);
        db.Offers.Add(offer);
        await db.SaveChangesAsync();

        offer.UpdateSellerSku("SKU-2", now);
        var cleared = offer.SetOrderQuantityLimits(null, null, now);
        Assert.True(cleared.IsSuccess);
        await db.SaveChangesAsync();

        db.ChangeTracker.Clear();
        var persisted = await db.Offers.AsNoTracking().SingleAsync(x => x.OfferId == offer.OfferId);
        Assert.Equal("SKU-2", persisted.SellerSku);
        Assert.True(persisted.Status == Tooba.Offer.Domain.ValueObjects.OfferStatus.Draft);
        Assert.Null(persisted.MinimumOrderQuantity);
        Assert.Null(persisted.MaximumOrderQuantity);
        await tx.RollbackAsync();
    }

    private static OfferDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<OfferDbContext>();
        ToobaNpgsql.ConfigureModuleContext(options, Connection, OfferDbContext.Schema, typeof(OfferDbContext));
        return new OfferDbContext(options.Options);
    }

    private static (bool Ok, string Reason) ValidateTarget()
    {
        try
        {
            using var connection = new NpgsqlConnection(Connection);
            connection.Open();
            using var command = new NpgsqlCommand("SELECT current_database()", connection);
            var database = (string?)command.ExecuteScalar();
            return string.Equals(database, "tooba_alpha", StringComparison.Ordinal)
                ? (true, "ok") : (false, "TB283 target is not tooba_alpha");
        }
        catch (Exception ex) { return (false, $"TB283 target unavailable: {ex.GetType().Name}"); }
    }
}
