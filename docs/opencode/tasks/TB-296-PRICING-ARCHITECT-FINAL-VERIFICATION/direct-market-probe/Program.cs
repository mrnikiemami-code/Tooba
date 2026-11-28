using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Tooba.Offer.Infrastructure.Adapters;
using Tooba.Offer.Infrastructure.Persistence;
using Tooba.Persistence;
using Tooba.Pricing.Contracts.Seller;
using Tooba.Pricing.Contracts.Errors;
using Tooba.Pricing.Infrastructure.Adapters;
using Tooba.Pricing.Infrastructure.Persistence;
using Tooba.StoreContext.Contracts.Current;

try
{
    if (args.Length != 3 || !Guid.TryParse(args[0], out var offerId)
        || !Guid.TryParse(args[1], out var sellerId) || string.IsNullOrWhiteSpace(args[2]))
        throw new InvalidOperationException("Expected isolated offer id, seller id and trusted-market positive control.");
    var raw = Environment.GetEnvironmentVariable("TOOBA_R11_CONNECTION")
        ?? throw new InvalidOperationException("No locally supplied existing DEV connection.");
    var connection = new NpgsqlConnectionStringBuilder(raw);
    if (connection.Database != "tooba_alpha" || !new[] { "127.0.0.1", "localhost", "::1" }.Contains(connection.Host))
        throw new InvalidOperationException("Refusing non-local or non-tooba_alpha database.");
    // Defense in depth: if the fail-closed branch regresses, database still rejects mutations.
    connection.Options = "-c default_transaction_read_only=on";
    connection.CommandTimeout = 20;
    connection.Timeout = 10;
    await using var witness = new NpgsqlConnection(connection.ConnectionString);
    await witness.OpenAsync();
    await using var ident = new NpgsqlCommand("select current_database() || '|' || coalesce(inet_server_addr()::text,'local') || '|' || coalesce(inet_server_port()::text,'local') || '|' || extract(epoch from pg_postmaster_start_time())::text || '|' || current_setting('transaction_read_only')", witness);
    var identity = (string?)await ident.ExecuteScalarAsync();
    if (identity is null || !identity.EndsWith("|on", StringComparison.Ordinal))
        throw new InvalidOperationException("Database read-only witness missing.");

    var pricingOptions = new DbContextOptionsBuilder<PricingDbContext>();
    var offerOptions = new DbContextOptionsBuilder<OfferDbContext>();
    ToobaNpgsql.ConfigureModuleContext(pricingOptions, connection.ConnectionString, PricingDbContext.Schema, typeof(PricingDbContext));
    ToobaNpgsql.ConfigureModuleContext(offerOptions, connection.ConnectionString, OfferDbContext.Schema, typeof(OfferDbContext));
    await using var pricing = new PricingDbContext(pricingOptions.Options);
    await using var offers = new OfferDbContext(offerOptions.Options);
    var lookup = new OfferStore(offers);
    using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(45));
    var token = timeout.Token;
    var offer = await lookup.FindOfferAsync(offerId, token);
    if (offer is null || offer.SellerPartyId != sellerId) throw new InvalidOperationException("Real fixture identity mismatch.");
    var guard = new OpenPricingUseCaseGuard();
    var positive = new PriceDirectory(pricing, guard, lookup, storeCommerce: new Context(new StoreCommerceContext(args[2], "IRR", offer.Channel.ToString())));
    var visible = await positive.ListBasePricesAsync(offerId, token);
    if (visible.Count == 0) throw new InvalidOperationException("Positive control did not see actual authored price.");
    var observations = new List<object>();
    foreach (var scenario in new[] { new Context(null), new Context(new StoreCommerceContext("", "IRR", offer.Channel.ToString())) })
    {
        var directory = new PriceDirectory(pricing, guard, lookup, storeCommerce: scenario);
        var rows = await directory.ListBasePricesAsync(offerId, token);
        foreach (var clientMarket in new string?[] { null, args[2] })
        {
            var result = await directory.SetPriceAsync(new SetSellerOfferPrice(offerId, sellerId, 123, "IRR", clientMarket, "absent", null, Guid.NewGuid()), token);
            var code = result.IsFailure ? result.FirstError.Code : "UNEXPECTED_SUCCESS";
            observations.Add(new { context = scenario.Current is null ? "null" : "empty", clientMarketSupplied = clientMarket is not null, errorCode = code, readRows = rows.Count });
            if (rows.Count != 0 || code != PricingErrorCodes.MarketInvalid)
                throw new InvalidOperationException("Missing market did not fail closed.");
        }
    }
    Console.WriteLine("R11_MARKET_JSON=" + JsonSerializer.Serialize(new { identity, offerId, positiveRows = visible.Count, observations, pass = true }));
    return 0;
}
catch (Exception ex)
{
    // Never print a connection string, full command line or exception text from the provider.
    Console.Error.WriteLine("R11 missing-market probe stopped: " + ex.GetType().Name);
    return 1;
}

sealed record Context(StoreCommerceContext? Current) : ICurrentStoreCommerceContext;
