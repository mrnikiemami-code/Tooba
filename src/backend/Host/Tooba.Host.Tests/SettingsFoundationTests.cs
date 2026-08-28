using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Testcontainers.PostgreSql;
using Tooba.AccessControl.Application;
using Tooba.AccessControl.Domain;
using Tooba.BuildingBlocks;
using Tooba.Host.OperatorProfile;
using Tooba.Host.Preferences;
using Tooba.Host.Seller;
using Tooba.Host.Settings;
using Tooba.Host.Storefront;
using Tooba.Identity.Application;
using Tooba.OperatorProfile.Application;
using Tooba.OperatorProfile.Infrastructure;
using Tooba.OperatorProfile.Infrastructure.Persistence;
using Tooba.Party.Application;
using Tooba.Party.Infrastructure;
using Tooba.Party.Infrastructure.Persistence;
using Tooba.Persistence;
using Tooba.UserPreference.Application;
using Tooba.UserPreference.Infrastructure;
using Tooba.UserPreference.Infrastructure.Persistence;
using Xunit;

namespace Tooba.Host.Tests;

/// <summary>قفل قرارداد تنظیمات مشتری/فروشنده/اپراتور، مجوزها، و دانهٔ Development.</summary>
public sealed class SettingsFoundationTests
{
    private readonly PostgreSqlContainer? _container;
    private readonly bool _available;

    public SettingsFoundationTests()
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
    public void Permission_catalog_includes_seller_settings_capabilities()
    {
        var ids = PermissionCatalog.All.Select(p => p.PermissionId).ToHashSet(StringComparer.Ordinal);
        Assert.Contains("seller.settings.view", ids);
        Assert.Contains("seller.settings.manage", ids);
        Assert.True(PermissionCatalog.IsDelegable("seller.settings.view"));
        Assert.True(PermissionCatalog.IsDelegable("seller.settings.manage"));
        Assert.Equal("Seller", PermissionCatalog.Require("seller.settings.view").Module);
    }

    [Fact]
    public void Settings_http_routes_are_wired()
    {
        var root = FindRepoRoot();
        var seller = File.ReadAllText(Path.Combine(root, "src", "backend", "Host", "Tooba.Host", "Seller", "SellerSettingsEndpoints.cs"));
        Assert.Contains("/v1/seller/settings", seller, StringComparison.Ordinal);
        Assert.Contains("seller.settings.view", seller, StringComparison.Ordinal);
        Assert.Contains("seller.settings.manage", seller, StringComparison.Ordinal);
        Assert.Contains("canManage", seller, StringComparison.Ordinal);
        Assert.Contains("SellerPanelAccess.RequireAuthorizedAsync", seller, StringComparison.Ordinal);

        var preference = File.ReadAllText(Path.Combine(root, "src", "backend", "Host", "Tooba.Host", "Preferences", "UserPreferenceEndpoints.cs"));
        Assert.Contains("/v1/customer/preferences", preference, StringComparison.Ordinal);
        Assert.Contains("/v1/admin/operator/preferences", preference, StringComparison.Ordinal);

        var uiPreference = File.ReadAllText(Path.Combine(root, "src", "backend", "Host", "Tooba.Host", "Preferences", "UiPreferenceEndpoints.cs"));
        Assert.Contains("/v1/admin/ui-preferences", uiPreference, StringComparison.Ordinal);
        Assert.Contains("AdminPanelAccess.RequireAuthorizedAsync", uiPreference, StringComparison.Ordinal);

        var operatorProfile = File.ReadAllText(Path.Combine(root, "src", "backend", "Host", "Tooba.Host", "OperatorProfile", "OperatorProfileEndpoints.cs"));
        Assert.Contains("/v1/admin/operator/profile", operatorProfile, StringComparison.Ordinal);
        Assert.Contains("AdminPanelAccess.RequireAuthorizedAsync", operatorProfile, StringComparison.Ordinal);

        var program = File.ReadAllText(Path.Combine(root, "src", "backend", "Host", "Tooba.Host", "Program.cs"));
        Assert.Contains("MapSellerSettingsEndpoints", program, StringComparison.Ordinal);
        Assert.Contains("MapUserPreferenceEndpoints", program, StringComparison.Ordinal);
        Assert.Contains("MapUiPreferenceEndpoints", program, StringComparison.Ordinal);
        Assert.Contains("MapOperatorProfileEndpoints", program, StringComparison.Ordinal);
    }

    [Fact]
    public void Preference_and_operator_contracts_are_own_only()
    {
        Assert.DoesNotContain("OwnerUserId", typeof(UserPreferenceWriteRequest).GetProperties().Select(x => x.Name));
        Assert.DoesNotContain("OwnerUserId", typeof(UserPreferenceWrite).GetProperties().Select(x => x.Name));
        Assert.DoesNotContain("OwnerUserId", typeof(OperatorProfileWriteRequest).GetProperties().Select(x => x.Name));
        Assert.Equal("user_preference", UserPreferenceDbContext.Schema);
        Assert.Equal("operator_profile", OperatorProfileDbContext.Schema);
        Assert.Equal(
            ["OwnerUserId", "Locale", "CreatedAt", "UpdatedAt"],
            typeof(Tooba.UserPreference.Domain.UserPreference).GetProperties().Select(x => x.Name).ToArray());
    }

    [Fact]
    public void Mobile_operator_role_seed_does_not_grant_settings_manage()
    {
        var source = File.ReadAllText(Path.Combine(
            FindRepoRoot(), "src", "backend", "Host", "Tooba.Host", "AccessControl", "AccessControlDevelopmentSeed.cs"));
        Assert.DoesNotContain("seller.settings.manage", source, StringComparison.Ordinal);
        Assert.Contains("order.handle", source, StringComparison.Ordinal);
    }

    [SkippableFact]
    public async Task Customer_preference_update_reload_and_foreign_isolation()
    {
        await using var db = await OpenPreferenceAsync();
        var directory = new UserPreferenceDirectory(db);
        var a = Guid.NewGuid();
        var b = Guid.NewGuid();
        Assert.Null(await directory.GetAsync(a, CancellationToken.None));
        var saved = await directory.UpsertAsync(a, new UserPreferenceWrite("en"), CancellationToken.None);
        Assert.Equal("en", saved.Locale);
        var reloaded = await directory.GetAsync(a, CancellationToken.None);
        Assert.Equal("en", reloaded!.Locale);
        await directory.UpsertAsync(b, new UserPreferenceWrite("fa"), CancellationToken.None);
        Assert.Equal("en", (await directory.GetAsync(a, CancellationToken.None))!.Locale);
        Assert.Equal("fa", (await directory.GetAsync(b, CancellationToken.None))!.Locale);
    }

    [SkippableFact]
    public async Task Operator_profile_own_get_put_persists()
    {
        await using var db = await OpenOperatorAsync();
        var directory = new OperatorProfileDirectory(db);
        var owner = Guid.NewGuid();
        Assert.Null(await directory.GetAsync(owner, CancellationToken.None));
        var saved = await directory.UpsertAsync(
            owner,
            new OperatorProfileWrite("مدیر تست", "مدیر", "تست", "بیو"),
            CancellationToken.None);
        Assert.Equal("مدیر تست", saved.DisplayName);
        var reloaded = await directory.GetAsync(owner, CancellationToken.None);
        Assert.Equal("بیو", reloaded!.Bio);
        var other = Guid.NewGuid();
        await directory.UpsertAsync(other, new OperatorProfileWrite("اپراتور دیگر", null, null, null), CancellationToken.None);
        Assert.Equal("مدیر تست", (await directory.GetAsync(owner, CancellationToken.None))!.DisplayName);
    }

    [SkippableFact]
    public async Task Seller_organization_profile_get_put_and_person_reject()
    {
        await using var db = await OpenPartyAsync();
        var directory = new PartyDirectory(db);
        var org = await directory.CreateOrganizationAsync("فروشگاه تست", "Legal Test", CancellationToken.None);
        var updated = await directory.UpdateOrganizationProfileAsync(
            org.PartyId,
            new OrganizationProfileWrite(
                "فروشگاه تست ۲",
                "Legal 2",
                "توضیح",
                "02111111111",
                "support@test.local",
                "تهران"),
            CancellationToken.None);
        Assert.Equal("فروشگاه تست ۲", updated.DisplayName);
        Assert.Equal("توضیح", updated.Description);
        var loaded = await directory.GetOrganizationProfileAsync(org.PartyId, CancellationToken.None);
        Assert.Equal("02111111111", loaded!.SupportPhone);

        var person = await directory.CreatePersonAsync("شخص تست", CancellationToken.None);
        Assert.Null(await directory.GetOrganizationProfileAsync(person.PartyId, CancellationToken.None));
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            directory.UpdateOrganizationProfileAsync(
                person.PartyId,
                new OrganizationProfileWrite("x", null, null, null, null, null),
                CancellationToken.None));
    }

    [SkippableFact]
    public async Task Seller_settings_capability_allow_and_deny()
    {
        var seller = Guid.NewGuid();
        var owner = Guid.NewGuid();
        var employee = Guid.NewGuid();
        var access = new SelectiveAccessControlDirectory(
            (owner, seller, "seller.settings.view"),
            (owner, seller, "seller.settings.manage"),
            (employee, seller, "seller.settings.view"));

        await SellerSettingsEndpoints.EnsureSellerCapabilityAsync(
            owner, seller, "seller.settings.view", access, CancellationToken.None);
        await SellerSettingsEndpoints.EnsureSellerCapabilityAsync(
            owner, seller, "seller.settings.manage", access, CancellationToken.None);

        var denied = await Assert.ThrowsAsync<PlatformHttpException>(() =>
            SellerSettingsEndpoints.EnsureSellerCapabilityAsync(
                employee, seller, "seller.settings.manage", access, CancellationToken.None));
        Assert.Equal(403, denied.StatusCode);
    }

    [Fact]
    public async Task Seller_foreign_actor_denied_by_panel_access()
    {
        var telemetry = new AuthorizationInstrumentation();
        var audit = new InMemoryAuthorizationSecurityEventSink();
        var adapter = new InMemoryAuthorizationAdapter(telemetry, audit);
        IAuthorizationGuard guard = new AuthorizationGuard(adapter);
        var sellerA = Guid.Parse("01a030d1-40cb-7000-8abe-6d31739956c5");
        var sellerB = Guid.Parse("01a030d1-40db-7000-b90c-a0705133f0eb");
        var actorA = Guid.Parse("bbbbbbbb-bbbb-4bbb-8bbb-bbbbbbbbbbb1");
        await adapter.WriteAsync(
            new AuthorizationRelationshipWrite
            {
                Subject = AuthorizationSubject.ForUser(actorA),
                Resource = new AuthorizationResource { Type = AuthorizationObjectTypes.Party, Id = sellerA.ToString("D") },
                Relation = AuthorizationRelations.Member,
            },
            CancellationToken.None);
        var denied = await Assert.ThrowsAsync<PlatformHttpException>(() =>
            SellerPanelAccess.AuthorizeActorForSellerAsync(guard, actorA, sellerB, CancellationToken.None));
        Assert.Equal(403, denied.StatusCode);
    }

    [SkippableFact]
    public async Task Settings_development_seed_is_idempotent()
    {
        await using var partyDb = await OpenPartyAsync();
        await using var preferenceDb = await OpenPreferenceAsync();
        await using var operatorDb = await OpenOperatorAsync();

        var parties = new PartyDirectory(partyDb);
        var org = await parties.CreateOrganizationAsync(
            SellerDevActorBootstrap.SellerADisplayName,
            "Arman Legal",
            CancellationToken.None);

        var services = new ServiceCollection();
        services.AddSingleton(partyDb);
        services.AddSingleton<IPartyDirectory>(parties);
        services.AddSingleton(preferenceDb);
        services.AddSingleton<IUserPreferenceDirectory>(new UserPreferenceDirectory(preferenceDb));
        services.AddSingleton(operatorDb);
        services.AddSingleton<IOperatorProfileDirectory>(new OperatorProfileDirectory(operatorDb));
        services.AddSingleton<IHostEnvironment>(new StaticHostEnvironment("Development"));
        await using var provider = services.BuildServiceProvider();

        // Admin snapshot خالی است؛ ترجیح مهمان و پروفایل سازمانی باید دو بار ایمن باشند.
        await SettingsFoundationDevelopmentSeed.ApplyAsync(provider);
        await SettingsFoundationDevelopmentSeed.ApplyAsync(provider);

        var profile = await parties.GetOrganizationProfileAsync(org.PartyId, CancellationToken.None);
        Assert.Equal(SettingsFoundationDevelopmentSeed.SellerASupportPhone, profile!.SupportPhone);
        var guestPref = await preferenceDb.Preferences.AsNoTracking()
            .Where(x => x.OwnerUserId == StorefrontCheckoutComposer.StorefrontGuestActorId)
            .ToListAsync();
        Assert.Single(guestPref);
        Assert.Equal("fa", guestPref[0].Locale);
    }

    private async Task<UserPreferenceDbContext> OpenPreferenceAsync()
    {
        Skip.If(!_available || _container is null, "Docker/Testcontainers PostgreSQL is not available.");
        var options = new DbContextOptionsBuilder<UserPreferenceDbContext>();
        ToobaNpgsql.ConfigureModuleContext(
            options,
            _container!.GetConnectionString(),
            UserPreferenceDbContext.Schema,
            typeof(UserPreferenceDbContext));
        var db = new UserPreferenceDbContext(options.Options);
        await db.Database.MigrateAsync();
        return db;
    }

    private async Task<OperatorProfileDbContext> OpenOperatorAsync()
    {
        Skip.If(!_available || _container is null, "Docker/Testcontainers PostgreSQL is not available.");
        var options = new DbContextOptionsBuilder<OperatorProfileDbContext>();
        ToobaNpgsql.ConfigureModuleContext(
            options,
            _container!.GetConnectionString(),
            OperatorProfileDbContext.Schema,
            typeof(OperatorProfileDbContext));
        var db = new OperatorProfileDbContext(options.Options);
        await db.Database.MigrateAsync();
        return db;
    }

    private async Task<PartyDbContext> OpenPartyAsync()
    {
        Skip.If(!_available || _container is null, "Docker/Testcontainers PostgreSQL is not available.");
        var options = new DbContextOptionsBuilder<PartyDbContext>();
        ToobaNpgsql.ConfigureModuleContext(
            options,
            _container!.GetConnectionString(),
            PartyDbContext.Schema,
            typeof(PartyDbContext));
        var db = new PartyDbContext(options.Options);
        await db.Database.MigrateAsync();
        return db;
    }

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

    private sealed class SelectiveAccessControlDirectory : FakeAccessControlDirectory
    {
        private readonly HashSet<(Guid UserId, Guid SellerId, string PermissionId)> _grants;

        public SelectiveAccessControlDirectory(params (Guid UserId, Guid SellerId, string PermissionId)[] grants)
        {
            _grants = grants.ToHashSet();
        }

        public override Task<EffectiveAccessDto> GetEffectiveAccessAsync(
            Guid userId,
            AccessOwnerScope owner,
            CancellationToken cancellationToken)
        {
            var permissions = _grants
                .Where(g => g.UserId == userId && g.SellerId == owner.OwnerScopeId)
                .Select(g =>
                {
                    var def = PermissionCatalog.Require(g.PermissionId);
                    return new EffectivePermissionDto(
                        g.PermissionId,
                        def.Module,
                        AccessScopeKind.GlobalWithinOwner,
                        null,
                        ["test-role"],
                        false);
                })
                .ToList();
            return Task.FromResult(new EffectiveAccessDto(
                userId,
                owner.Kind,
                owner.OwnerScopeId,
                permissions,
                permissions.Count == 0 ? [] : ["test-role"]));
        }
    }

    private sealed class StaticHostEnvironment : IHostEnvironment
    {
        public StaticHostEnvironment(string name) => EnvironmentName = name;
        public string EnvironmentName { get; set; }
        public string ApplicationName { get; set; } = "tests";
        public string ContentRootPath { get; set; } = AppContext.BaseDirectory;
        public Microsoft.Extensions.FileProviders.IFileProvider ContentRootFileProvider { get; set; } =
            new Microsoft.Extensions.FileProviders.NullFileProvider();
    }
}
