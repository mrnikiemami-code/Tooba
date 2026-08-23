using Microsoft.EntityFrameworkCore;
using Npgsql;
using Testcontainers.PostgreSql;
using Tooba.BuildingBlocks;
using Tooba.Catalog.Domain;
using Tooba.Offer.Domain;
using Tooba.Order.Domain;
using Tooba.Persistence;
using Tooba.Pricing.Domain;
using Tooba.Tax.Application;
using Tooba.Tax.Domain;
using Tooba.Tax.Infrastructure;
using Tooba.Tax.Infrastructure.Persistence;
using Xunit;

namespace Tooba.Host.Tests;

/// <summary>
/// پوشش foundation مالیات: جدا از Pricing، چهار outcome متمایز، تاریخ مؤثر، و تصویر سفارش تغییرناپذیر.
/// </summary>
[Collection("PostgresSerial")]
public sealed class TaxFoundationTests : IAsyncLifetime
{
    private PostgreSqlContainer? _container;
    private bool _dockerAvailable;

    /// <inheritdoc />
    public async Task InitializeAsync()
    {
        try
        {
            _container = new PostgreSqlBuilder()
                .WithImage("postgres:16-alpine")
                .WithDatabase("tooba_tax_a")
                .WithUsername("tooba")
                .WithPassword("dev-placeholder")
                .Build();
            await _container.StartAsync();
            _dockerAvailable = true;
        }
        catch (Exception)
        {
            _dockerAvailable = false;
        }
    }

    /// <inheritdoc />
    public async Task DisposeAsync()
    {
        if (_container is not null)
        {
            await _container.DisposeAsync();
        }
    }

    /// <summary>
    /// قیمت نوشته‌شده و کالا مبلغ مالیات ندارند؛ چهار نتیجه یکی نیستند.
    /// </summary>
    [Fact]
    public void Pricing_and_catalog_do_not_own_tax_amounts_and_outcomes_stay_distinct()
    {
        Assert.DoesNotContain("TaxAmount", typeof(CatalogProduct).GetProperties().Select(p => p.Name));
        Assert.DoesNotContain("TaxAmount", typeof(SellerOffer).GetProperties().Select(p => p.Name));
        Assert.DoesNotContain("TaxAmount", typeof(AuthoredPrice).GetProperties().Select(p => p.Name));
        Assert.True(Money.Create(10m, "IRR").IsTaxExclusive);
        Assert.NotEqual(TaxOutcome.Exempt, TaxOutcome.ZeroRated);
        Assert.NotEqual(TaxOutcome.ZeroRated, TaxOutcome.NoApplicableRule);
        Assert.NotEqual(TaxOutcome.NoApplicableRule, TaxOutcome.CalculationError);
        Assert.Equal("tax", TaxDbContext.Schema);
        var domain = File.ReadAllText(Path.Combine(FindRepoRoot(), "src", "backend", "Modules", "Tax", "Tooba.Tax.Domain", "TaxDomain.cs"));
        Assert.DoesNotContain("0.09", domain, StringComparison.Ordinal);
        Assert.DoesNotContain("1405", domain, StringComparison.Ordinal);
        Assert.DoesNotContain("MassTransit", File.ReadAllText(Path.Combine(FindRepoRoot(), "src", "backend", "Modules", "Tax", "Tooba.Tax.Domain", "Tooba.Tax.Domain.csproj")), StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// محاسبه روی Postgres: درصد، معاف، نرخ صفر، نبودن قاعده، ابهام، ایزولهٔ Tenant و گرد کردن قطعی.
    /// </summary>
    [SkippableFact]
    public async Task Tax_calculator_distinguishes_outcomes_and_isolates_tenants_on_postgres()
    {
        Skip.If(!_dockerAvailable || _container is null, "Docker/Testcontainers PostgreSQL is not available.");
        var csA = _container.GetConnectionString();
        await using (var admin = new Npgsql.NpgsqlConnection(csA))
        {
            await admin.OpenAsync();
            await using var cmd = admin.CreateCommand();
            cmd.CommandText = "SELECT 1 FROM pg_database WHERE datname = 'tooba_tax_b'";
            if (await cmd.ExecuteScalarAsync() is null)
            {
                await using var create = admin.CreateCommand();
                create.CommandText = "CREATE DATABASE tooba_tax_b";
                await create.ExecuteNonQueryAsync();
            }
        }

        var csB = new Npgsql.NpgsqlConnectionStringBuilder(csA) { Database = "tooba_tax_b" }.ConnectionString;
        var commerceA = new FixedCommerceContext();
        commerceA.Assign(OutboxTestContextFactory.SingleStore("tenant-tax-a", "tenant-tax-a"));
        var commerceB = new FixedCommerceContext();
        commerceB.Assign(OutboxTestContextFactory.SingleStore("tenant-tax-b", "tenant-tax-b"));
        await using var taxA = CreateTaxDb(csA, commerceA);
        await using var taxB = CreateTaxDb(csB, commerceB);
        await taxA.Database.MigrateAsync();
        await taxB.Database.MigrateAsync();
        var dirA = new TaxDirectory(taxA, new OpenTaxUseCaseGuard());
        var dirB = new TaxDirectory(taxB, new OpenTaxUseCaseGuard());
        var start = DateTimeOffset.Parse("2026-01-01T00:00:00Z");
        var standard = await dirA.CreateCategoryAsync("standard", "استاندارد", CancellationToken.None);
        var exemptCat = await dirA.CreateCategoryAsync("exempt", "معاف", CancellationToken.None);
        var zeroCat = await dirA.CreateCategoryAsync("zero", "نرخ صفر", CancellationToken.None);
        var offerTaxable = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaa1");
        var offerExempt = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaa2");
        var offerZero = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaa3");
        var offerMissing = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaa4");
        await dirA.AssignOfferCategoryAsync(offerTaxable, standard.CategoryId, CancellationToken.None);
        await dirA.AssignOfferCategoryAsync(offerExempt, exemptCat.CategoryId, CancellationToken.None);
        await dirA.AssignOfferCategoryAsync(offerZero, zeroCat.CategoryId, CancellationToken.None);
        var percent = await dirA.CreateRuleAsync("IR-NAT", "IR", standard.CategoryId, TaxRuleKind.Percentage, 0.09m, start, null, 10, TaxOverridePolicy.Disabled, CancellationToken.None);
        await dirA.ActivateRuleAsync(percent.RuleId, CancellationToken.None);
        var exempt = await dirA.CreateRuleAsync("IR-NAT", "IR", exemptCat.CategoryId, TaxRuleKind.Exempt, 0m, start, null, 10, TaxOverridePolicy.Disabled, CancellationToken.None);
        await dirA.ActivateRuleAsync(exempt.RuleId, CancellationToken.None);
        var zero = await dirA.CreateRuleAsync("IR-NAT", "IR", zeroCat.CategoryId, TaxRuleKind.ZeroRated, 0m, start, null, 10, TaxOverridePolicy.Disabled, CancellationToken.None);
        await dirA.ActivateRuleAsync(zero.RuleId, CancellationToken.None);

        var taxable = await dirA.CalculateAsync(Req(offerTaxable, 100000m), CancellationToken.None);
        Assert.Equal(TaxOutcome.Taxable, taxable.Outcome);
        Assert.Equal(9000m, taxable.TaxAmount);
        Assert.Equal(109000m, taxable.TaxInclusiveAmount);
        Assert.Equal(percent.RuleId, taxable.RuleId);

        var exempted = await dirA.CalculateAsync(Req(offerExempt, 100000m), CancellationToken.None);
        Assert.Equal(TaxOutcome.Exempt, exempted.Outcome);
        Assert.Equal(0m, exempted.TaxAmount);

        var zeroed = await dirA.CalculateAsync(Req(offerZero, 100000m), CancellationToken.None);
        Assert.Equal(TaxOutcome.ZeroRated, zeroed.Outcome);
        Assert.Equal(0m, zeroed.TaxAmount);
        Assert.NotEqual(exempted.Outcome, zeroed.Outcome);

        var missing = await dirA.CalculateAsync(Req(offerMissing, 100000m), CancellationToken.None);
        Assert.Equal(TaxOutcome.NoApplicableRule, missing.Outcome);

        var clientInject = await dirA.CalculateAsync(Req(offerTaxable, 100000m) with { AllowTrustedOverride = false, TrustedOverrideRate = 0.50m }, CancellationToken.None);
        Assert.Equal(9000m, clientInject.TaxAmount);

        var overlap = await dirA.CreateRuleAsync("IR-NAT", "IR", standard.CategoryId, TaxRuleKind.Percentage, 0.05m, start, null, 10, TaxOverridePolicy.Disabled, CancellationToken.None);
        await dirA.ActivateRuleAsync(overlap.RuleId, CancellationToken.None);
        var ambiguous = await dirA.CalculateAsync(Req(offerTaxable, 100000m), CancellationToken.None);
        Assert.Equal(TaxOutcome.CalculationError, ambiguous.Outcome);

        var isolated = await dirB.CalculateAsync(Req(offerTaxable, 100000m), CancellationToken.None);
        Assert.Equal(TaxOutcome.NoApplicableRule, isolated.Outcome);
        Assert.DoesNotContain("TaxAmount", typeof(OrderLine).GetProperties().Select(p => p.Name).Where(n => n is "TaxAmount"));
        Assert.Contains("TaxAmountSnapshot", typeof(OrderLine).GetProperties().Select(p => p.Name));
    }

    private static TaxCalculationRequest Req(Guid offerId, decimal amount) =>
        new(offerId, "IR-NAT", "IR", "IRR", amount, 1, DateTimeOffset.Parse("2026-06-01T00:00:00Z"), null, false, null);

    private static TaxDbContext CreateTaxDb(string connectionString, ICurrentCommerceContext commerce)
    {
        var modules = new IOutboxModuleRegistration[] { new TaxOutboxRegistration() };
        var serializer = new JsonIntegrationEventSerializer(modules);
        var interceptor = new OutboxSaveChangesInterceptor(commerce, modules, serializer);
        var options = new DbContextOptionsBuilder<TaxDbContext>();
        ToobaNpgsql.ConfigureModuleContext(options, connectionString, TaxDbContext.Schema, typeof(TaxDbContext));
        options.AddInterceptors(interceptor);
        return new TaxDbContext(options.Options);
    }

    private static string FindRepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            if (File.Exists(Path.Combine(dir.FullName, "AGENTS.md")))
            {
                return dir.FullName;
            }

            dir = dir.Parent;
        }

        throw new InvalidOperationException("Repository root not found.");
    }
}
