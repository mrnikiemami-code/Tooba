using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Tooba.AccessControl.Contracts.Development;
using Tooba.AccessControl.Contracts.Enums;
using Tooba.AccessControl.Domain.Aggregates;
using Tooba.AccessControl.Infrastructure.Authorization;
using Tooba.AccessControl.Infrastructure.Development;
using Tooba.AccessControl.Infrastructure.Directories;
using Tooba.AccessControl.Infrastructure.Observability;
using Tooba.AccessControl.Infrastructure.Persistence;
using Tooba.BuildingBlocks;
using Tooba.Catalog.Contracts.Ports;
using Xunit;

namespace Tooba.Host.Tests;

/// <summary>
/// TB-282-R9 — behavioral recovery of process-local InMemory tuples from durable AccessControl rows.
/// </summary>
public sealed class AccessControlInMemoryProjectionRecovery282R9Tests
{
    private static readonly Guid AdminUser = Guid.Parse("aaaaaaaa-aaaa-4aaa-8aaa-aaaaaaaa0009");
    private static readonly Guid SellerUser = Guid.Parse("bbbbbbbb-bbbb-4bbb-8bbb-bbbbbbbb0009");
    private static readonly Guid OtherUser = Guid.Parse("cccccccc-cccc-4ccc-8ccc-cccccccc0009");
    private static readonly Guid SellerParty = Guid.Parse("dddddddd-dddd-4ddd-8ddd-dddddddd0009");
    private const string TenantId = "store-alpha";

    [Fact]
    public async Task Recover_from_durable_assignments_restores_tenant_and_party_view_on_empty_engine()
    {
        await using var db = CreateDb();
        SeedDurableAssignments(db);
        var adapter = CreateAdapter();
        var recovery = CreateRecovery(db, adapter, Environments.Development, "InMemory");

        // Empty engine: denied before recovery.
        Assert.Equal(AuthorizationDecisionKind.Deny, (await CheckTenantAsync(adapter, AdminUser)).Kind);
        Assert.Equal(AuthorizationDecisionKind.Deny, (await CheckPartyAsync(adapter, SellerUser, SellerParty)).Kind);

        var result = await recovery.RecoverAsync(TenantId, CancellationToken.None);
        Assert.True(result.EngineEligible);
        Assert.Equal(2, result.UsersProjected);
        Assert.Equal(1, result.TenantMembershipsProjected);
        Assert.Equal(1, result.PartyMembershipsProjected);

        Assert.Equal(AuthorizationDecisionKind.Allow, (await CheckTenantAsync(adapter, AdminUser)).Kind);
        Assert.Equal(AuthorizationDecisionKind.Allow, (await CheckPartyAsync(adapter, SellerUser, SellerParty)).Kind);
        // Unassigned actor stays denied.
        Assert.Equal(AuthorizationDecisionKind.Deny, (await CheckTenantAsync(adapter, OtherUser)).Kind);
        Assert.Equal(AuthorizationDecisionKind.Deny, (await CheckPartyAsync(adapter, OtherUser, SellerParty)).Kind);
        // Cross-scope isolation: seller does not gain tenant#view; admin does not gain foreign party.
        Assert.Equal(AuthorizationDecisionKind.Deny, (await CheckTenantAsync(adapter, SellerUser)).Kind);
        Assert.Equal(
            AuthorizationDecisionKind.Deny,
            (await CheckPartyAsync(adapter, AdminUser, SellerParty)).Kind);
    }

    [Fact]
    public async Task Recover_is_noop_when_mode_is_not_inmemory_or_environment_is_not_development()
    {
        await using var db = CreateDb();
        SeedDurableAssignments(db);
        var adapter = CreateAdapter();

        var spice = CreateRecovery(db, adapter, Environments.Development, "SpiceDb");
        var spiceResult = await spice.RecoverAsync(TenantId, CancellationToken.None);
        Assert.False(spiceResult.EngineEligible);
        Assert.Equal(0, spiceResult.UsersProjected);
        Assert.Equal(AuthorizationDecisionKind.Deny, (await CheckTenantAsync(adapter, AdminUser)).Kind);

        var prod = CreateRecovery(db, adapter, Environments.Production, "InMemory");
        var prodResult = await prod.RecoverAsync(TenantId, CancellationToken.None);
        Assert.False(prodResult.EngineEligible);
        Assert.Equal(AuthorizationDecisionKind.Deny, (await CheckTenantAsync(adapter, AdminUser)).Kind);
    }

    [Fact]
    public async Task Recover_does_not_invent_membership_when_no_durable_assignment()
    {
        await using var db = CreateDb();
        // Role exists but no assignment for AdminUser.
        db.Roles.Add(new AccessRole
        {
            Id = Guid.NewGuid(),
            TenantId = TenantId,
            OwnerScopeKind = AccessOwnerScopeKind.Platform,
            OwnerScopeId = null,
            Name = "Platform Admin",
            Code = "platform-admin",
            IsSystem = true,
            IsMutable = false,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow,
        });
        await db.SaveChangesAsync();

        var adapter = CreateAdapter();
        var recovery = CreateRecovery(db, adapter, Environments.Development, "InMemory");
        var result = await recovery.RecoverAsync(TenantId, CancellationToken.None);
        Assert.True(result.EngineEligible);
        Assert.Equal(0, result.UsersProjected);
        Assert.Equal(0, result.TenantMembershipsProjected);
        Assert.Equal(AuthorizationDecisionKind.Deny, (await CheckTenantAsync(adapter, AdminUser)).Kind);
    }

    private static AccessControlDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<AccessControlDbContext>()
            .UseInMemoryDatabase("tb282r9-" + Guid.NewGuid().ToString("N"))
            .Options;
        return new AccessControlDbContext(options);
    }

    private static InMemoryAuthorizationAdapter CreateAdapter() =>
        new(new AuthorizationInstrumentation(), new InMemoryAuthorizationSecurityEventSink());

    private static IAccessControlInMemoryProjectionRecovery CreateRecovery(
        AccessControlDbContext db,
        InMemoryAuthorizationAdapter adapter,
        string environmentName,
        string mode)
    {
        var env = new FakeHostEnvironment { EnvironmentName = environmentName };
        var options = Options.Create(new SpiceDbAuthorizationOptions { Mode = mode });
        var directory = new AccessControlDirectory(
            db,
            adapter,
            new AccessControlInstrumentation(),
            new EmptyScopeLookup());
        return new AccessControlInMemoryProjectionRecovery(env, options, db, directory, adapter);
    }

    private static void SeedDurableAssignments(AccessControlDbContext db)
    {
        var adminRole = new AccessRole
        {
            Id = Guid.NewGuid(),
            TenantId = TenantId,
            OwnerScopeKind = AccessOwnerScopeKind.Platform,
            OwnerScopeId = null,
            Name = "Platform Admin",
            Code = "platform-admin",
            IsSystem = true,
            IsMutable = false,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow,
        };
        var sellerRole = new AccessRole
        {
            Id = Guid.NewGuid(),
            TenantId = TenantId,
            OwnerScopeKind = AccessOwnerScopeKind.Seller,
            OwnerScopeId = SellerParty,
            Name = "Seller Owner",
            Code = "seller-owner",
            IsSystem = true,
            IsMutable = false,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow,
        };
        db.Roles.AddRange(adminRole, sellerRole);
        db.RolePermissions.Add(new RolePermission
        {
            Id = Guid.NewGuid(),
            RoleId = adminRole.Id,
            PermissionId = "story.view",
            ScopeKind = AccessScopeKind.GlobalWithinOwner,
            Enabled = true,
        });
        db.RolePermissions.Add(new RolePermission
        {
            Id = Guid.NewGuid(),
            RoleId = sellerRole.Id,
            PermissionId = "story.view",
            ScopeKind = AccessScopeKind.GlobalWithinOwner,
            Enabled = true,
        });
        db.Assignments.Add(new UserRoleAssignment
        {
            Id = Guid.NewGuid(),
            UserId = AdminUser,
            RoleId = adminRole.Id,
            OwnerScopeKind = AccessOwnerScopeKind.Platform,
            OwnerScopeId = null,
            AssignedAt = DateTimeOffset.UtcNow,
        });
        db.Assignments.Add(new UserRoleAssignment
        {
            Id = Guid.NewGuid(),
            UserId = SellerUser,
            RoleId = sellerRole.Id,
            OwnerScopeKind = AccessOwnerScopeKind.Seller,
            OwnerScopeId = SellerParty,
            AssignedAt = DateTimeOffset.UtcNow,
        });
        db.SaveChanges();
    }

    private static Task<AuthorizationDecision> CheckTenantAsync(IAuthorizationService service, Guid userId) =>
        service.CanAsync(
            new AuthorizationCheck
            {
                Subject = AuthorizationSubject.ForUser(userId),
                Resource = new AuthorizationResource { Type = AuthorizationObjectTypes.Tenant, Id = TenantId },
                Permission = AuthorizationRelations.View,
                CallContext = new AuthorizationCallContext { Edition = ToobaEdition.SingleStore, TenantId = TenantId },
            },
            CancellationToken.None);

    private static Task<AuthorizationDecision> CheckPartyAsync(
        IAuthorizationService service,
        Guid userId,
        Guid partyId) =>
        service.CanAsync(
            new AuthorizationCheck
            {
                Subject = AuthorizationSubject.ForUser(userId),
                Resource = new AuthorizationResource
                {
                    Type = AuthorizationObjectTypes.Party,
                    Id = partyId.ToString("D"),
                },
                Permission = AuthorizationRelations.View,
                CallContext = new AuthorizationCallContext { Edition = ToobaEdition.SingleStore },
            },
            CancellationToken.None);

    private sealed class EmptyScopeLookup : IAccessControlScopeResourceLookup
    {
        public Task<IReadOnlyList<AccessControlScopeResourceCategory>> ListCategoriesAsync(
            string? search,
            CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<AccessControlScopeResourceCategory>>([]);

        public Task<IReadOnlyList<AccessControlScopeResourceBrand>> ListBrandsAsync(
            string? search,
            CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<AccessControlScopeResourceBrand>>([]);

        public Task<IReadOnlyList<AccessControlScopeResourceProduct>> ListProductsAsync(
            string? search,
            CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<AccessControlScopeResourceProduct>>([]);

        public Task<bool> CategoryExistsAsync(Guid categoryId, CancellationToken cancellationToken) =>
            Task.FromResult(false);

        public Task<IReadOnlyDictionary<Guid, string>> GetCategoryNamesAsync(
            IReadOnlyCollection<Guid> categoryIds,
            CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyDictionary<Guid, string>>(new Dictionary<Guid, string>());
    }
}
