using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Tooba.Persistence;
using Tooba.Tax.Contracts.Ports;
using Tooba.Tax.Domain.Enums;
using Tooba.Tax.Infrastructure.Adapters;
using Tooba.Tax.Infrastructure.Persistence;

// Test-only console; no HTTP routes, no seed changes, no production service registration.
try
{
    if (args.Length != 5 || !new[] { "attach", "detach", "inspect" }.Contains(args[0])
        || !Guid.TryParse(args[1], out var id) || id == Guid.Empty
        || !args[2].StartsWith("R11-CHECKOUT-", StringComparison.Ordinal)
        || !Guid.TryParse(args[2]["R11-CHECKOUT-".Length..], out _)
        || !Guid.TryParse(args[3], out var categoryId) || categoryId == Guid.Empty
        || string.IsNullOrWhiteSpace(args[4]))
        throw new InvalidOperationException("Expected action, synthetic offer ID, exact run SKU, existing category ID and market.");
    var action = args[0];
    var raw = Environment.GetEnvironmentVariable("TOOBA_R11_CONNECTION")
        ?? throw new InvalidOperationException("Existing DEV connection required.");
    var cs = new NpgsqlConnectionStringBuilder(raw);
    if (cs.Database != "tooba_alpha" || !new[] { "localhost", "127.0.0.1", "::1" }.Contains(cs.Host))
        throw new InvalidOperationException("Refusing any nonlocal or non-DEV database.");
    cs.Timeout = 10;
    cs.CommandTimeout = 20;
    using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(45));
    var ct = timeout.Token;
    // Cross-owner reads only; prove target identity before entering the Tax owner write boundary.
    await using var witness = new NpgsqlConnection(cs.ConnectionString);
    await witness.OpenAsync(ct);
    await using var identity = new NpgsqlCommand("select current_database() || '|' || extract(epoch from pg_postmaster_start_time())::text", witness);
    var dbIdentity = (string?)await identity.ExecuteScalarAsync(ct);
    await using var target = new NpgsqlCommand("select count(*) from offer.offers where offer_id=@id and seller_sku=@sku", witness);
    target.Parameters.AddWithValue("id", id);
    target.Parameters.AddWithValue("sku", args[2]);
    if ((long)(await target.ExecuteScalarAsync(ct))! != 1)
        throw new InvalidOperationException("Exact synthetic offer not found; never mutate an existing commercial classification.");
    var options = new DbContextOptionsBuilder<TaxDbContext>();
    ToobaNpgsql.ConfigureModuleContext(options, cs.ConnectionString, TaxDbContext.Schema, typeof(TaxDbContext));
    await using var db = new TaxDbContext(options.Options);
    await using var tx = await db.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable, ct);
    var category = await db.Categories.AsNoTracking().SingleOrDefaultAsync(x => x.CategoryId == categoryId, ct)
        ?? throw new InvalidOperationException("Existing category required; no new category creation.");
    var rules = await db.Rules.AsNoTracking().Where(x => x.CategoryId == categoryId && x.Market == args[4] && x.Status == TaxRuleStatus.Active).ToListAsync(ct);
    if (!rules.Any(x => x.IsEffectiveAt(DateTimeOffset.UtcNow)))
        throw new InvalidOperationException("Existing effective rule required; no rule creation.");
    var before = await db.OfferClassifications.SingleOrDefaultAsync(x => x.OfferId == id, ct);
    if (before is not null && before.CategoryId != categoryId)
        throw new InvalidOperationException("Existing classification differs; do not overwrite.");
    if (action == "attach")
    {
        if (before is not null)
            throw new InvalidOperationException("Classification must initially be absent.");
        var directory = new TaxDirectory(db, new OpenTaxUseCaseGuard());
        var seed = new TaxDevelopmentSeedGateway(directory, directory);
        var result = await seed.EnsureDevelopmentOfferCategoryAsync(new EnsureDevelopmentOfferCategory(id, category.Code, category.DisplayName), ct);
        if (result.IsFailure) throw new InvalidOperationException("Tax-owned classification failed.");
    }
    else if (action == "detach")
    {
        // Invoke only after exact checkout/cart/order/payment cleanup, before deleting the Offer.
        await using var linked = new NpgsqlCommand("select (select count(*) from cart.cart_lines where offer_id=@id) + (select count(*) from \"order\".order_lines where offer_id=@id)", witness);
        linked.Parameters.AddWithValue("id", id);
        if ((long)(await linked.ExecuteScalarAsync(ct))! != 0)
            throw new InvalidOperationException("Linked cart/order rows still exist; cleanup refused.");
        if (before is not null)
        {
            db.OfferClassifications.Remove(before);
            await db.SaveChangesAsync(ct);
        }
    }
    var after = await db.OfferClassifications.AsNoTracking().SingleOrDefaultAsync(x => x.OfferId == id, ct);
    if (action == "attach" && after?.CategoryId != categoryId || action == "detach" && after is not null)
        throw new InvalidOperationException("Tax readback failed.");
    await tx.CommitAsync(ct);
    Console.WriteLine("R11_TAX_JSON=" + JsonSerializer.Serialize(new {
        action, dbIdentity, offerId = id, sku = args[2], categoryId, before = before?.CategoryId, after = after?.CategoryId,
        effectiveRuleCount = rules.Count(x => x.IsEffectiveAt(DateTimeOffset.UtcNow)), pass = true
    }));
    return 0;
}
catch (Exception ex)
{
    Console.Error.WriteLine("R11 Tax fixture stopped: " + ex.GetType().Name);
    return 1;
}
