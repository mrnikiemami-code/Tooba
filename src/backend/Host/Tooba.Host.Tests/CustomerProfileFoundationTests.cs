using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Testcontainers.PostgreSql;
using Tooba.CustomerProfile.Application;
using Tooba.CustomerProfile.Infrastructure;
using Tooba.CustomerProfile.Infrastructure.Persistence;
using Tooba.Host.Customer;
using Tooba.Host.CustomerProfile;
using Tooba.Host.Storefront;
using Tooba.Persistence;
using Xunit;

namespace Tooba.Host.Tests;

/// <summary>قفل قرارداد، حریم خصوصی، اعتبارسنجی و seed پروفایل مشتری.</summary>
public sealed class CustomerProfileFoundationTests
{
    private readonly PostgreSqlContainer? _container;
    private readonly bool _available;

    public CustomerProfileFoundationTests()
    {
        try
        {
            _container = new PostgreSqlBuilder().Build();
            _container.StartAsync().GetAwaiter().GetResult();
            _available = true;
        }
        catch
        {
            _available = false;
        }
    }

    [Fact]
    public void Entity_does_not_store_identity_credentials()
    {
        var names = typeof(Tooba.CustomerProfile.Domain.CustomerProfile).GetProperties().Select(x => x.Name).ToArray();
        Assert.Equal(
            ["OwnerUserId", "FirstName", "LastName", "DisplayName", "BirthDate", "Bio", "CreatedAt", "UpdatedAt"],
            names);
        Assert.DoesNotContain("Email", names);
        Assert.DoesNotContain("Password", names);
        Assert.DoesNotContain("Mobile", names);
        Assert.Equal("customer_profile", CustomerProfileDbContext.Schema);
    }

    [Fact]
    public void Http_and_write_contracts_have_no_owner_or_identity_authority()
    {
        Assert.DoesNotContain("OwnerUserId", typeof(CustomerProfileWriteRequest).GetProperties().Select(x => x.Name));
        Assert.DoesNotContain("OwnerUserId", typeof(CustomerProfileWrite).GetProperties().Select(x => x.Name));
        Assert.DoesNotContain("Email", typeof(CustomerProfileWriteRequest).GetProperties().Select(x => x.Name));
        Assert.DoesNotContain("ContactMobile", typeof(CustomerProfileWriteRequest).GetProperties().Select(x => x.Name));
        Assert.DoesNotContain("Password", typeof(CustomerProfileWriteRequest).GetProperties().Select(x => x.Name));
    }

    [Fact]
    public void Endpoint_uses_session_and_supports_profile_update()
    {
        var source = File.ReadAllText(Path.Combine(
            FindRepoRoot(), "src", "backend", "Host", "Tooba.Host", "Customer", "CustomerPanelEndpoints.cs"));
        Assert.Contains("MapPut(\"/profile\"", source, StringComparison.Ordinal);
        Assert.Contains("session.IsAuthenticated", source, StringComparison.Ordinal);
        Assert.Contains("StatusCodes.Status401Unauthorized", source, StringComparison.Ordinal);
    }

    [SkippableFact]
    public async Task Actor_can_read_and_update_own_profile()
    {
        await using var db = await OpenAsync();
        var directory = new CustomerProfileDirectory(db);
        var owner = Guid.NewGuid();
        Assert.Null(await directory.GetAsync(owner, CancellationToken.None));
        var saved = await directory.UpsertAsync(owner, SampleWrite("علی رضایی"), CancellationToken.None);
        Assert.Equal("علی", saved.FirstName);
        Assert.Equal("رضایی", saved.LastName);
        Assert.Equal("علی رضایی", saved.DisplayName);
        var updated = await directory.UpsertAsync(owner, SampleWrite("سارا احمدی", bio: "بیو"), CancellationToken.None);
        Assert.Equal("سارا احمدی", updated.DisplayName);
        Assert.Equal("بیو", updated.Bio);
    }

    [SkippableFact]
    public async Task Actor_cannot_update_foreign_profile()
    {
        await using var db = await OpenAsync();
        var directory = new CustomerProfileDirectory(db);
        var a = Guid.NewGuid();
        var b = Guid.NewGuid();
        await directory.UpsertAsync(a, SampleWrite("کاربر A"), CancellationToken.None);
        var rowB = await db.Profiles.SingleAsync(x => x.OwnerUserId == a);
        Assert.NotEqual(b, rowB.OwnerUserId);
        await directory.UpsertAsync(b, SampleWrite("کاربر B"), CancellationToken.None);
        var profileA = await directory.GetAsync(a, CancellationToken.None);
        var profileB = await directory.GetAsync(b, CancellationToken.None);
        Assert.Equal("کاربر A", profileA!.DisplayName);
        Assert.Equal("کاربر B", profileB!.DisplayName);
    }

    [SkippableFact]
    public async Task Invalid_values_are_rejected()
    {
        await using var db = await OpenAsync();
        var directory = new CustomerProfileDirectory(db);
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            directory.UpsertAsync(Guid.NewGuid(), SampleWrite("ab"), CancellationToken.None));
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            directory.UpsertAsync(Guid.NewGuid(), SampleWrite("نام معتبر", bio: new string('x', 201)), CancellationToken.None));
    }

    [SkippableFact]
    public async Task Development_seed_is_idempotent()
    {
        await using var db = await OpenAsync();
        var services = new ServiceCollection();
        services.AddSingleton(db);
        await using var provider = services.BuildServiceProvider();
        await CustomerProfileDevelopmentSeed.ApplyAsync(provider);
        await CustomerProfileDevelopmentSeed.ApplyAsync(provider);
        var actor = StorefrontCheckoutComposer.StorefrontGuestActorId;
        var rows = await db.Profiles.AsNoTracking().Where(x => x.OwnerUserId == actor).ToListAsync();
        Assert.Single(rows);
        Assert.Equal("مشتری نمایشی توبا", rows[0].DisplayName);
    }

    private async Task<CustomerProfileDbContext> OpenAsync()
    {
        Skip.If(!_available || _container is null, "Docker/Testcontainers PostgreSQL is not available.");
        var options = new DbContextOptionsBuilder<CustomerProfileDbContext>();
        ToobaNpgsql.ConfigureModuleContext(
            options,
            _container!.GetConnectionString(),
            CustomerProfileDbContext.Schema,
            typeof(CustomerProfileDbContext));
        var db = new CustomerProfileDbContext(options.Options);
        await db.Database.MigrateAsync();
        return db;
    }

    private static CustomerProfileWrite SampleWrite(string displayName, string? bio = null) =>
        new(displayName, null, null, "1403/06/04", bio);

    private static string FindRepoRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "AGENTS.md")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new InvalidOperationException("repo root not found");
    }
}
