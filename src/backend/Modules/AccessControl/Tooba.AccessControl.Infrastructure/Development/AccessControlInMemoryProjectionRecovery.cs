using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Tooba.AccessControl.Application.Models;
using Tooba.AccessControl.Application.Ports;
using Tooba.AccessControl.Contracts.Development;
using Tooba.AccessControl.Contracts.Enums;
using Tooba.AccessControl.Infrastructure.Authorization;
using Tooba.AccessControl.Infrastructure.Persistence;
using Tooba.BuildingBlocks;

namespace Tooba.AccessControl.Infrastructure.Development;

/// <summary>
/// AccessControl-owned recovery: durable assignments → ephemeral InMemory tuples only.
/// </summary>
/// <remarks>Public for focused Host.Tests behavioral harness; Host still consumes only the Contracts interface.</remarks>
public sealed class AccessControlInMemoryProjectionRecovery : IAccessControlInMemoryProjectionRecovery
{
    private readonly IHostEnvironment _environment;
    private readonly SpiceDbAuthorizationOptions _options;
    private readonly AccessControlDbContext _db;
    private readonly IAccessControlDirectory _directory;
    private readonly IAuthorizationTupleWriter _tuples;

    /// <summary>Constructs the Development InMemory projection recovery seam.</summary>
    public AccessControlInMemoryProjectionRecovery(
        IHostEnvironment environment,
        IOptions<SpiceDbAuthorizationOptions> options,
        AccessControlDbContext db,
        IAccessControlDirectory directory,
        IAuthorizationTupleWriter tuples)
    {
        _environment = environment;
        _options = options.Value;
        _db = db;
        _directory = directory;
        _tuples = tuples;
    }

    /// <inheritdoc />
    public async Task<AccessControlInMemoryProjectionRecoveryResult> RecoverAsync(
        string tenantId,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tenantId);

        if (!_environment.IsDevelopment()
            || !string.Equals(_options.Mode, "InMemory", StringComparison.OrdinalIgnoreCase))
        {
            return new AccessControlInMemoryProjectionRecoveryResult(false, 0, 0, 0);
        }

        // Durable authority only: non-archived roles scoped to this tenant.
        var roles = await _db.Roles.AsNoTracking()
            .Where(r => r.TenantId == tenantId && !r.IsArchived)
            .Select(r => new { r.Id, r.OwnerScopeKind, r.OwnerScopeId })
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        if (roles.Count == 0)
        {
            return new AccessControlInMemoryProjectionRecoveryResult(true, 0, 0, 0);
        }

        var roleIds = roles.Select(r => r.Id).ToList();
        var assignments = await _db.Assignments.AsNoTracking()
            .Where(a => roleIds.Contains(a.RoleId))
            .Select(a => new { a.UserId, a.OwnerScopeKind, a.OwnerScopeId })
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var usersProjected = 0;
        var tenantMemberships = 0;
        var partyMemberships = 0;
        var seenSync = new HashSet<(Guid UserId, AccessOwnerScopeKind Kind, Guid? OwnerScopeId)>();

        foreach (var row in assignments)
        {
            var key = (row.UserId, row.OwnerScopeKind, row.OwnerScopeId);
            if (!seenSync.Add(key))
            {
                continue;
            }

            if (row.UserId == Guid.Empty)
            {
                continue;
            }

            var owner = new AccessOwnerScope(row.OwnerScopeKind, row.OwnerScopeId, tenantId);
            // Sync reads durable GetEffectiveAccess and writes ONLY process-local capability tuples.
            await _directory.SyncUserCapabilityTuplesAsync(row.UserId, owner, cancellationToken)
                .ConfigureAwait(false);
            usersProjected++;

            if (row.OwnerScopeKind == AccessOwnerScopeKind.Platform)
            {
                await _tuples.WriteAsync(
                    new AuthorizationRelationshipWrite
                    {
                        Subject = AuthorizationSubject.ForUser(row.UserId),
                        Resource = new AuthorizationResource
                        {
                            Type = AuthorizationObjectTypes.Tenant,
                            Id = tenantId,
                        },
                        Relation = AuthorizationRelations.Member,
                        Operation = AuthorizationRelationshipOperation.Touch,
                    },
                    cancellationToken).ConfigureAwait(false);
                tenantMemberships++;
            }
            else if (row.OwnerScopeKind == AccessOwnerScopeKind.Seller
                     && row.OwnerScopeId is Guid partyId
                     && partyId != Guid.Empty)
            {
                await _tuples.WriteAsync(
                    new AuthorizationRelationshipWrite
                    {
                        Subject = AuthorizationSubject.ForUser(row.UserId),
                        Resource = new AuthorizationResource
                        {
                            Type = AuthorizationObjectTypes.Party,
                            Id = partyId.ToString("D"),
                        },
                        Relation = AuthorizationRelations.Member,
                        Operation = AuthorizationRelationshipOperation.Touch,
                    },
                    cancellationToken).ConfigureAwait(false);
                partyMemberships++;
            }
        }

        return new AccessControlInMemoryProjectionRecoveryResult(
            true,
            usersProjected,
            tenantMemberships,
            partyMemberships);
    }
}
