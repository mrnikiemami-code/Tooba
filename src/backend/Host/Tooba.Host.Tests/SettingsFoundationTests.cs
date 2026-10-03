using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Testcontainers.PostgreSql;
using Tooba.AccessControl.Application;
using Tooba.AccessControl.Contracts.Enums;
using Tooba.AccessControl.Infrastructure.Authorization;
using Tooba.BuildingBlocks;
using Tooba.BuildingBlocks.Security;
using Tooba.OperatorProfile.Endpoints.Admin;
using Tooba.AccessControl.Infrastructure.Development.Seller;
using Tooba.Host.Security.Seller;
using Tooba.Catalog.Infrastructure.Development;
using Tooba.Host.Composition;
using Tooba.Order.Contracts.Fulfillment;
using Tooba.Order.Application.Storefront.Models;
using Tooba.Party.Infrastructure.Development;
using Tooba.AddressBook.Contracts.Dtos;
using Tooba.AddressBook.Contracts.Ports;
using Tooba.Cart.Application.Ports;
using Tooba.Fulfillment.Contracts.Shipping;
using Tooba.Identity.Application.Models;
using Tooba.Identity.Application.Options;
using Tooba.Identity.Application.Ports;
using Tooba.OperatorProfile.Application.Models;
using Tooba.OperatorProfile.Application.Ports;
using Tooba.OperatorProfile.Infrastructure.Persistence;
using Tooba.OperatorProfile.Infrastructure.Profiles;
using Tooba.Party.Application;
using Tooba.Party.Infrastructure;
using Tooba.Party.Infrastructure.Persistence;
using Tooba.Persistence;
using Tooba.UserPreference.Application;
using Tooba.UserPreference.Endpoints.Customer;
using Tooba.UserPreference.Infrastructure;
using Tooba.UserPreference.Infrastructure.Persistence;
using Xunit;

using Tooba.AccessControl.Application.Models;
using Tooba.AccessControl.Application.Permissions;
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
        var seller = File.ReadAllText(Path.Combine(root, "src", "backend", "Modules", "Party", "Tooba.Party.Endpoints", "Seller", "PartySellerSettingsEndpoints.cs"));
        Assert.Contains("/v1/seller/settings", seller, StringComparison.Ordinal);
        Assert.Contains("canManage", seller, StringComparison.Ordinal);
        Assert.Contains("MapGet", seller, StringComparison.Ordinal);
        Assert.Contains("MapPut", seller, StringComparison.Ordinal);

        // Capability literals now live in the Host security adapter, not in the module Endpoints.
        var authorizer = File.ReadAllText(Path.Combine(root, "src", "backend", "Host", "Tooba.Host", "Security", "Seller", "HostPartySellerAuthorizer.cs"));
        Assert.Contains("seller.settings.view", authorizer, StringComparison.Ordinal);
        Assert.Contains("seller.settings.manage", authorizer, StringComparison.Ordinal);
        Assert.Contains("ISellerPanelAccess", authorizer, StringComparison.Ordinal);
        Assert.Contains("IPlatformEffectiveAccessReader", authorizer, StringComparison.Ordinal);

        // The evacuated Host seller settings surface must be gone.
        Assert.False(File.Exists(Path.Combine(root, "src", "backend", "Host", "Tooba.Host", "Seller", "SellerSettingsEndpoints.cs")));

        Assert.Contains("/v1/customer/preferences", File.ReadAllText(Path.Combine(root, "src", "backend", "Modules", "UserPreference", "Tooba.UserPreference.Endpoints", "UserPreferenceEndpointModule.cs")), StringComparison.Ordinal);
        Assert.Contains("/v1/admin/operator/preferences", File.ReadAllText(Path.Combine(root, "src", "backend", "Modules", "UserPreference", "Tooba.UserPreference.Endpoints", "UserPreferenceEndpointModule.cs")), StringComparison.Ordinal);

        var uiPreference = File.ReadAllText(Path.Combine(root, "src", "backend", "Modules", "UserPreference", "Tooba.UserPreference.Endpoints", "Admin", "UiPreferenceAdminEndpoints.cs"));
        Assert.Contains("/v1/admin/ui-preferences", File.ReadAllText(Path.Combine(root, "src", "backend", "Modules", "UserPreference", "Tooba.UserPreference.Endpoints", "UserPreferenceEndpointModule.cs")), StringComparison.Ordinal);
        Assert.Contains("IUserPreferenceAdminAuthorizer", uiPreference, StringComparison.Ordinal);
        Assert.DoesNotContain("AdminPanelAccess.RequireAuthorizedAsync", uiPreference, StringComparison.Ordinal);

        var operatorProfile = File.ReadAllText(Path.Combine(root, "src", "backend", "Modules", "OperatorProfile", "Tooba.OperatorProfile.Endpoints", "Admin", "OperatorProfileAdminEndpoints.cs"));
        Assert.Contains("/v1/admin/operator/profile", File.ReadAllText(Path.Combine(root, "src", "backend", "Modules", "OperatorProfile", "Tooba.OperatorProfile.Endpoints", "OperatorProfileEndpointModule.cs")), StringComparison.Ordinal);

        var holds = File.ReadAllText(Path.Combine(root, "src", "backend", "Modules", "Catalog", "Tooba.Catalog.Endpoints", "Admin", "Settings", "HoldPolicySettingsEndpoints.cs"));
        Assert.Contains("/v1/admin/settings/hold-policy", holds, StringComparison.Ordinal);
        var holdComposer = File.ReadAllText(Path.Combine(root, "src", "backend", "Modules", "Catalog", "Tooba.Catalog.Application", "Settings", "HoldPolicy", "HoldPolicySettingsComposer.cs"));
        Assert.Contains("مدت نگهداری سبد خرید", holdComposer, StringComparison.Ordinal);
        Assert.Contains("مهلت پرداخت آنلاین", holdComposer, StringComparison.Ordinal);
        Assert.DoesNotContain("AdminPanelAccess.RequireAuthorizedAsync", operatorProfile, StringComparison.Ordinal);
        Assert.Contains("IOperatorProfileAdminAuthorizer", operatorProfile, StringComparison.Ordinal);
        Assert.Contains("ApiResponseFactory", operatorProfile, StringComparison.Ordinal);

        var program = File.ReadAllText(Path.Combine(root, "src", "backend", "Host", "Tooba.Host", "Program.cs"));
        Assert.Contains("MapPartyEndpoints", program, StringComparison.Ordinal);
        Assert.DoesNotContain("MapSellerSettingsEndpoints", program, StringComparison.Ordinal);
        Assert.Contains("MapUserPreferenceModuleEndpoints", program, StringComparison.Ordinal);
        Assert.DoesNotContain("MapUserPreferenceEndpoints()", program, StringComparison.Ordinal);
        Assert.DoesNotContain("MapUiPreferenceEndpoints()", program, StringComparison.Ordinal);
        Assert.Contains("MapOperatorProfileModuleEndpoints", program, StringComparison.Ordinal);
        Assert.DoesNotContain("MapOperatorProfileEndpoints()", program, StringComparison.Ordinal);
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

    [Fact]
    public async Task Seller_settings_capability_allow_and_deny()
    {
        var seller = Guid.NewGuid();
        var owner = Guid.NewGuid();
        var employee = Guid.NewGuid();
        var reader = new SelectiveEffectiveAccessReader(
            (owner, seller, "seller.settings.view"),
            (owner, seller, "seller.settings.manage"),
            (employee, seller, "seller.settings.view"));

        var ownerAuthorizer = new HostPartySellerAuthorizer(new StubSellerPanelAccess(owner, seller), reader);
        var ownerView = await ownerAuthorizer.RequireViewAsync(Context(), CancellationToken.None);
        Assert.True(ownerView.CanManage, "owner holds seller.settings.manage");
        await ownerAuthorizer.RequireManageAsync(Context(), CancellationToken.None);

        // Employee holds view but NOT manage: read route must still succeed with CanManage=false.
        var employeeAuthorizer = new HostPartySellerAuthorizer(new StubSellerPanelAccess(employee, seller), reader);
        var employeeView = await employeeAuthorizer.RequireViewAsync(Context(), CancellationToken.None);
        Assert.False(employeeView.CanManage, "employee lacks seller.settings.manage");

        var denied = await Assert.ThrowsAsync<SemanticException>(() =>
            employeeAuthorizer.RequireManageAsync(Context(), CancellationToken.None));
        Assert.Equal("seller.authorization.denied", denied.Error.Code);
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
        var denied = await Assert.ThrowsAsync<SemanticException>(() =>
            SellerPanelAccess.AuthorizeActorForSellerAsync(guard, actorA, sellerB, new FixedCurrentEdition(ToobaEdition.SingleStore), CancellationToken.None));
        Assert.Equal("seller.authorization.denied", denied.Error.Code);
    }

    [SkippableFact]
    public async Task Settings_development_seed_is_idempotent()
    {
        await using var partyDb = await OpenPartyAsync();
        await using var preferenceDb = await OpenPreferenceAsync();
        await using var operatorDb = await OpenOperatorAsync();

        var parties = new PartyDirectory(partyDb);
        var org = await parties.CreateOrganizationAsync(
            WorkspaceDemoMarketplaceSeed.SellerADisplayName,
            "Arman Legal",
            CancellationToken.None);

        var services = new ServiceCollection();
        services.AddSingleton(partyDb);
        services.AddSingleton<IPartyDirectory>(parties);
        services.AddSingleton<Tooba.Party.Contracts.IPartyDevelopmentSeedGateway>(
            new StubPartyDevelopmentSeedGateway(org.PartyId, WorkspaceDemoMarketplaceSeed.SellerADisplayName));
        services.AddSingleton(preferenceDb);
        services.AddSingleton<IUserPreferenceDirectory>(new UserPreferenceDirectory(preferenceDb));
        services.AddSingleton(operatorDb);
        services.AddSingleton<IOperatorProfileDirectory>(new OperatorProfileDirectory(operatorDb));
        services.AddSingleton<IHostEnvironment>(new StaticHostEnvironment("Development"));
        await using var provider = services.BuildServiceProvider();

        // Admin snapshot خالی است؛ ترجیح مهمان و پروفایل سازمانی باید دو بار ایمن باشند.
        await SettingsFoundationDevelopmentSeedHost.ApplyAsync(provider);
        await SettingsFoundationDevelopmentSeedHost.ApplyAsync(provider);

        var profile = await parties.GetOrganizationProfileAsync(org.PartyId, CancellationToken.None);
        Assert.Equal(PartyOrganizationProfileDevelopmentSeed.SellerASupportPhone, profile!.SupportPhone);
        var guestPref = await preferenceDb.Preferences.AsNoTracking()
            .Where(x => x.OwnerUserId == StorefrontGuestActor.ActorId)
            .ToListAsync();
        Assert.Single(guestPref);
        Assert.Equal("fa", guestPref[0].Locale);
    }

    private sealed class StubPartyDevelopmentSeedGateway(Guid partyId, string expectedDisplayName)
        : Tooba.Party.Contracts.IPartyDevelopmentSeedGateway
    {
        public Task<Guid> ResolveDevelopmentSellerPartyAsync(
            string displayName,
            string? legalName,
            CancellationToken cancellationToken) => Task.FromResult(partyId);

        public Task<Guid> EnsureDevelopmentOrganizationAsync(
            string displayName,
            string? legalName,
            CancellationToken cancellationToken) => Task.FromResult(partyId);

        public Task EnsureDevelopmentOrganizationDisplayNamesAsync(
            IReadOnlyCollection<Tooba.Party.Contracts.DevelopmentOrganizationRename> renames,
            CancellationToken cancellationToken) => Task.CompletedTask;

        public Task<Guid?> FindDevelopmentOrganizationByDisplayNameAsync(
            string displayName,
            CancellationToken cancellationToken) =>
            Task.FromResult<Guid?>(
                string.Equals(displayName, expectedDisplayName, StringComparison.Ordinal) ? partyId : null);

        public Task<Guid?> FindDevelopmentMembershipSellerPartyAsync(
            Guid userId,
            CancellationToken cancellationToken) => Task.FromResult<Guid?>(null);

        public Task EnsureDevelopmentMemberMembershipAsync(
            Guid userId,
            Guid sellerPartyId,
            CancellationToken cancellationToken) => Task.CompletedTask;
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

    private static HttpContext Context() =>
        new DefaultHttpContext();

    /// <summary>
    /// درز خنثی دسترسی پنل فروشنده: Actor/SellerPartyId ثابت را بدون موتور مجوز برمی‌گرداند.
    /// فقط برای تست درزهای نازک امنیتی Host استفاده می‌شود.
    /// </summary>
    private sealed class StubSellerPanelAccess(Guid actorUserId, Guid sellerPartyId) : ISellerPanelAccess
    {
        public Task<(Guid ActorUserId, Guid SellerPartyId)> RequireAuthorizedAsync(
            HttpRequest request,
            CancellationToken cancellationToken) =>
            Task.FromResult((actorUserId, sellerPartyId));
    }

    /// <summary>
    /// درز خنثی مجوز مؤثر بر پایهٔ گرنت‌های صریح؛ جایگزین دایرکتوری AccessControl در تست.
    /// </summary>
    private sealed class SelectiveEffectiveAccessReader
        : IPlatformEffectiveAccessReader
    {
        private readonly HashSet<(Guid UserId, Guid SellerId, string PermissionId)> _grants;

        public SelectiveEffectiveAccessReader(params (Guid UserId, Guid SellerId, string PermissionId)[] grants)
        {
            _grants = grants.ToHashSet();
        }

        public Task<IReadOnlyList<PlatformPermissionGrant>> GetEffectivePermissionsAsync(
            Guid userId,
            PlatformAccessOwnerKind ownerKind,
            Guid? ownerScopeId,
            CancellationToken cancellationToken)
        {
            var grants = _grants
                .Where(g => g.UserId == userId && g.SellerId == ownerScopeId)
                .Select(g => new PlatformPermissionGrant(
                    g.PermissionId,
                    PlatformAccessScopeKind.GlobalWithinOwner,
                    null,
                    false))
                .ToList();
            return Task.FromResult<IReadOnlyList<PlatformPermissionGrant>>(grants);
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

    private sealed class FixedCurrentEdition(ToobaEdition edition) : ICurrentEdition
    {
        public EditionContext? Current { get; } = new EditionContext(edition, "test");
    }
}