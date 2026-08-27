using Microsoft.EntityFrameworkCore;
using Tooba.AccessControl.Application;
using Tooba.AccessControl.Domain;
using Tooba.AccessControl.Infrastructure.Persistence;
using Tooba.BuildingBlocks;

namespace Tooba.AccessControl.Infrastructure;

/// <summary>
/// پیاده‌سازی دایرکتوری Access Control با SoT پیکربندی در PG و enforcement در SpiceDB.
/// </summary>
public sealed class AccessControlDirectory : IAccessControlDirectory
{
    private readonly AccessControlDbContext _db;
    private readonly IAuthorizationTupleWriter _tuples;
    private readonly AccessControlInstrumentation _telemetry;

    /// <summary>دایرکتوری را با DbContext و writer می‌سازد.</summary>
    public AccessControlDirectory(
        AccessControlDbContext db,
        IAuthorizationTupleWriter tuples,
        AccessControlInstrumentation telemetry)
    {
        _db = db;
        _tuples = tuples;
        _telemetry = telemetry;
    }

    /// <inheritdoc />
    public IReadOnlyList<PermissionDefinition> ListCatalog() => PermissionCatalog.All;

    /// <inheritdoc />
    public async Task<IReadOnlyList<AccessRoleDto>> ListRolesAsync(
        AccessOwnerScope owner,
        bool includeArchived,
        CancellationToken cancellationToken)
    {
        var query = ScopedRoles(owner);
        if (!includeArchived)
        {
            query = query.Where(r => !r.IsArchived);
        }

        var roles = await query.OrderBy(r => r.Name).ToListAsync(cancellationToken);
        return await MapRolesAsync(roles, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<AccessRoleDto?> GetRoleAsync(Guid roleId, AccessOwnerScope owner, CancellationToken cancellationToken)
    {
        var role = await RequireRoleAsync(roleId, owner, cancellationToken);
        return (await MapRolesAsync([role], cancellationToken))[0];
    }

    /// <inheritdoc />
    public async Task<AccessRoleDto> CreateRoleAsync(
        AccessOwnerScope owner,
        CreateAccessRoleCommand command,
        Guid actorUserId,
        string? traceId,
        CancellationToken cancellationToken)
    {
        ValidateOwner(owner);
        var now = DateTimeOffset.UtcNow;
        var role = new AccessRole
        {
            Id = Guid.NewGuid(),
            TenantId = owner.TenantId,
            OwnerScopeKind = owner.Kind,
            OwnerScopeId = owner.OwnerScopeId,
            Name = RequireText(command.Name, 128),
            Code = RequireCode(command.Code),
            Description = command.Description?.Trim() ?? string.Empty,
            IsSystem = false,
            IsMutable = true,
            IsArchived = false,
            CreatedAt = now,
            UpdatedAt = now,
        };

        if (await ScopedRoles(owner).AnyAsync(r => r.Code == role.Code && !r.IsArchived, cancellationToken))
        {
            throw new AccessControlException("access.role.code_conflict", "کد نقش تکراری است.");
        }

        _db.Roles.Add(role);
        await AuditAsync(actorUserId, "role.create", "role", role.Id.ToString("D"), owner.OwnerScopeId, string.Empty, role.Code, traceId, cancellationToken);
        await _db.SaveChangesAsync(cancellationToken);
        _telemetry.RecordRoleMutation();
        return (await MapRolesAsync([role], cancellationToken))[0];
    }

    /// <inheritdoc />
    public async Task<AccessRoleDto> UpdateRoleAsync(
        Guid roleId,
        AccessOwnerScope owner,
        UpdateAccessRoleCommand command,
        Guid actorUserId,
        string? traceId,
        CancellationToken cancellationToken)
    {
        var role = await RequireRoleAsync(roleId, owner, cancellationToken);
        EnsureMutable(role);
        var before = $"{role.Name}|{role.Description}";
        role.Name = RequireText(command.Name, 128);
        role.Description = command.Description?.Trim() ?? string.Empty;
        role.UpdatedAt = DateTimeOffset.UtcNow;
        await AuditAsync(actorUserId, "role.update", "role", role.Id.ToString("D"), owner.OwnerScopeId, before, $"{role.Name}|{role.Description}", traceId, cancellationToken);
        await _db.SaveChangesAsync(cancellationToken);
        _telemetry.RecordRoleMutation();
        return (await MapRolesAsync([role], cancellationToken))[0];
    }

    /// <inheritdoc />
    public async Task<AccessRoleDto> CloneRoleAsync(
        Guid roleId,
        AccessOwnerScope owner,
        CloneAccessRoleCommand command,
        Guid actorUserId,
        string? traceId,
        CancellationToken cancellationToken)
    {
        var source = await RequireRoleAsync(roleId, owner, cancellationToken);
        var created = await CreateRoleAsync(
            owner,
            new CreateAccessRoleCommand(command.Name, command.Code, command.Description ?? source.Description),
            actorUserId,
            traceId,
            cancellationToken);
        var grants = await GetRolePermissionsAsync(roleId, owner, cancellationToken);
        if (grants.Count > 0)
        {
            await SetRolePermissionsAsync(created.Id, owner, grants, actorUserId, traceId, cancellationToken);
        }

        await AuditAsync(actorUserId, "role.clone", "role", created.Id.ToString("D"), owner.OwnerScopeId, source.Id.ToString("D"), created.Id.ToString("D"), traceId, cancellationToken);
        return (await GetRoleAsync(created.Id, owner, cancellationToken))!;
    }

    /// <inheritdoc />
    public async Task ArchiveRoleAsync(
        Guid roleId,
        AccessOwnerScope owner,
        Guid actorUserId,
        string? traceId,
        CancellationToken cancellationToken)
    {
        var role = await RequireRoleAsync(roleId, owner, cancellationToken);
        EnsureMutable(role);
        if (role.IsSystem)
        {
            throw new AccessControlException("access.role.system_immutable", "نقش سیستمی قابل بایگانی نیست.");
        }

        role.IsArchived = true;
        role.UpdatedAt = DateTimeOffset.UtcNow;
        var assignees = await _db.Assignments.Where(a => a.RoleId == roleId).ToListAsync(cancellationToken);
        _db.Assignments.RemoveRange(assignees);
        await AuditAsync(actorUserId, "role.archive", "role", role.Id.ToString("D"), owner.OwnerScopeId, "active", "archived", traceId, cancellationToken);
        await _db.SaveChangesAsync(cancellationToken);
        foreach (var userId in assignees.Select(a => a.UserId).Distinct())
        {
            await SyncUserCapabilityTuplesAsync(userId, owner, cancellationToken);
        }

        _telemetry.RecordRoleMutation();
    }

    /// <inheritdoc />
    public async Task SetRolePermissionsAsync(
        Guid roleId,
        AccessOwnerScope owner,
        IReadOnlyList<RolePermissionGrant> grants,
        Guid actorUserId,
        string? traceId,
        CancellationToken cancellationToken)
    {
        var role = await RequireRoleAsync(roleId, owner, cancellationToken);
        EnsureMutable(role);
        await ValidateGrantsForOwnerAsync(owner, grants, cancellationToken);

        var existing = await _db.RolePermissions.Where(p => p.RoleId == roleId).ToListAsync(cancellationToken);
        var before = string.Join(",", existing.Where(x => x.Enabled).Select(x => x.PermissionId).OrderBy(x => x));
        _db.RolePermissions.RemoveRange(existing);

        foreach (var grant in grants.Where(g => g.Enabled))
        {
            _ = PermissionCatalog.Require(grant.PermissionId);
            _db.RolePermissions.Add(new RolePermission
            {
                Id = Guid.NewGuid(),
                RoleId = roleId,
                PermissionId = grant.PermissionId,
                ScopeKind = grant.ScopeKind,
                ScopeResourceId = grant.ScopeResourceId,
                Enabled = true,
            });
        }

        role.UpdatedAt = DateTimeOffset.UtcNow;
        var after = string.Join(",", grants.Where(g => g.Enabled).Select(g => g.PermissionId).OrderBy(x => x));
        await AuditAsync(actorUserId, "role.permissions", "role", roleId.ToString("D"), owner.OwnerScopeId, before, after, traceId, cancellationToken);
        await _db.SaveChangesAsync(cancellationToken);

        var userIds = await _db.Assignments.Where(a => a.RoleId == roleId).Select(a => a.UserId).Distinct().ToListAsync(cancellationToken);
        foreach (var userId in userIds)
        {
            await SyncUserCapabilityTuplesAsync(userId, owner, cancellationToken);
        }

        _telemetry.RecordRoleMutation();
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<RolePermissionGrant>> GetRolePermissionsAsync(
        Guid roleId,
        AccessOwnerScope owner,
        CancellationToken cancellationToken)
    {
        _ = await RequireRoleAsync(roleId, owner, cancellationToken);
        return await _db.RolePermissions
            .AsNoTracking()
            .Where(p => p.RoleId == roleId && p.Enabled)
            .OrderBy(p => p.PermissionId)
            .Select(p => new RolePermissionGrant(p.PermissionId, p.ScopeKind, p.ScopeResourceId, p.Enabled))
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<UserRoleAssignmentDto>> ListAssignmentsAsync(
        AccessOwnerScope owner,
        Guid? userId,
        CancellationToken cancellationToken)
    {
        var q = from a in _db.Assignments.AsNoTracking()
                join r in ScopedRoles(owner).AsNoTracking() on a.RoleId equals r.Id
                where a.OwnerScopeKind == owner.Kind && a.OwnerScopeId == owner.OwnerScopeId
                select new { a, r };
        if (userId is Guid uid)
        {
            q = q.Where(x => x.a.UserId == uid);
        }

        return await q.OrderBy(x => x.a.AssignedAt)
            .Select(x => new UserRoleAssignmentDto(
                x.a.Id,
                x.a.UserId,
                x.a.RoleId,
                x.r.Name,
                x.r.Code,
                x.a.OwnerScopeKind,
                x.a.OwnerScopeId,
                x.a.AssignedAt))
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<UserRoleAssignmentDto> AssignRoleAsync(
        AccessOwnerScope owner,
        Guid userId,
        Guid roleId,
        Guid actorUserId,
        string? traceId,
        CancellationToken cancellationToken)
    {
        if (userId == Guid.Empty)
        {
            throw new AccessControlException("access.user.invalid", "کاربر نامعتبر است.");
        }

        var role = await RequireRoleAsync(roleId, owner, cancellationToken);
        if (role.IsArchived)
        {
            throw new AccessControlException("access.role.archived", "نقش بایگانی‌شده قابل تخصیص نیست.");
        }

        var exists = await _db.Assignments.AnyAsync(
            a => a.UserId == userId && a.RoleId == roleId && a.OwnerScopeKind == owner.Kind && a.OwnerScopeId == owner.OwnerScopeId,
            cancellationToken);
        if (exists)
        {
            throw new AccessControlException("access.assignment.exists", "تخصیص تکراری است.");
        }

        var row = new UserRoleAssignment
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            RoleId = roleId,
            OwnerScopeKind = owner.Kind,
            OwnerScopeId = owner.OwnerScopeId,
            AssignedAt = DateTimeOffset.UtcNow,
        };
        _db.Assignments.Add(row);
        await _db.SaveChangesAsync(cancellationToken);
        await SyncUserCapabilityTuplesAsync(userId, owner, cancellationToken);
        await AuditAsync(actorUserId, "assignment.add", "assignment", row.Id.ToString("D"), owner.OwnerScopeId, string.Empty, $"{userId:D}:{role.Code}", traceId, cancellationToken);
        await _db.SaveChangesAsync(cancellationToken);
        _telemetry.RecordAssignmentMutation();
        return new UserRoleAssignmentDto(row.Id, userId, roleId, role.Name, role.Code, owner.Kind, owner.OwnerScopeId, row.AssignedAt);
    }

    /// <inheritdoc />
    public async Task RemoveAssignmentAsync(
        Guid assignmentId,
        AccessOwnerScope owner,
        Guid actorUserId,
        string? traceId,
        CancellationToken cancellationToken)
    {
        var row = await _db.Assignments.FirstOrDefaultAsync(
            a => a.Id == assignmentId && a.OwnerScopeKind == owner.Kind && a.OwnerScopeId == owner.OwnerScopeId,
            cancellationToken)
            ?? throw new AccessControlException("access.assignment.not_found", "تخصیص یافت نشد.");
        var userId = row.UserId;
        _db.Assignments.Remove(row);
        await AuditAsync(actorUserId, "assignment.remove", "assignment", assignmentId.ToString("D"), owner.OwnerScopeId, userId.ToString("D"), string.Empty, traceId, cancellationToken);
        await _db.SaveChangesAsync(cancellationToken);
        await SyncUserCapabilityTuplesAsync(userId, owner, cancellationToken);
        _telemetry.RecordAssignmentMutation();
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<SellerCeilingEntryDto>> GetSellerCeilingAsync(
        Guid sellerPartyId,
        CancellationToken cancellationToken)
    {
        var enabled = await _db.SellerCeilings.AsNoTracking()
            .Where(c => c.SellerPartyId == sellerPartyId && c.Enabled)
            .Select(c => c.PermissionId)
            .ToListAsync(cancellationToken);
        var set = enabled.ToHashSet(StringComparer.Ordinal);
        return PermissionCatalog.All
            .Where(p => p.Delegable)
            .Select(p => new SellerCeilingEntryDto(p.PermissionId, set.Contains(p.PermissionId), p.Delegable, p.Module))
            .ToList();
    }

    /// <inheritdoc />
    public async Task SetSellerCeilingAsync(
        Guid sellerPartyId,
        IReadOnlyList<(string PermissionId, bool Enabled)> entries,
        Guid actorUserId,
        string? traceId,
        CancellationToken cancellationToken)
    {
        foreach (var (permissionId, _) in entries)
        {
            var def = PermissionCatalog.Require(permissionId);
            if (!def.Delegable)
            {
                throw new AccessControlException("access.ceiling.not_delegable", $"مجوز پلتفرمی قابل سقف نیست: {permissionId}");
            }
        }

        var existing = await _db.SellerCeilings.Where(c => c.SellerPartyId == sellerPartyId).ToListAsync(cancellationToken);
        var before = string.Join(",", existing.Where(x => x.Enabled).Select(x => x.PermissionId).OrderBy(x => x));
        _db.SellerCeilings.RemoveRange(existing);
        var now = DateTimeOffset.UtcNow;
        foreach (var (permissionId, enabled) in entries.Where(e => e.Enabled))
        {
            _db.SellerCeilings.Add(new PlatformSellerCeiling
            {
                Id = Guid.NewGuid(),
                SellerPartyId = sellerPartyId,
                PermissionId = permissionId,
                Enabled = true,
                UpdatedAt = now,
            });
        }

        var after = string.Join(",", entries.Where(e => e.Enabled).Select(e => e.PermissionId).OrderBy(x => x));
        await AuditAsync(actorUserId, "ceiling.set", "seller", sellerPartyId.ToString("D"), sellerPartyId, before, after, traceId, cancellationToken);
        await _db.SaveChangesAsync(cancellationToken);

        var owner = new AccessOwnerScope(AccessOwnerScopeKind.Seller, sellerPartyId);
        var userIds = await _db.Assignments
            .Where(a => a.OwnerScopeKind == AccessOwnerScopeKind.Seller && a.OwnerScopeId == sellerPartyId)
            .Select(a => a.UserId)
            .Distinct()
            .ToListAsync(cancellationToken);
        foreach (var userId in userIds)
        {
            await SyncUserCapabilityTuplesAsync(userId, owner, cancellationToken);
        }

        _telemetry.RecordCeilingMutation();
    }

    /// <inheritdoc />
    public async Task<EffectiveAccessDto> GetEffectiveAccessAsync(
        Guid userId,
        AccessOwnerScope owner,
        CancellationToken cancellationToken)
    {
        var assignments = await (
            from a in _db.Assignments.AsNoTracking()
            join r in ScopedRoles(owner).AsNoTracking() on a.RoleId equals r.Id
            where a.UserId == userId && a.OwnerScopeKind == owner.Kind && a.OwnerScopeId == owner.OwnerScopeId && !r.IsArchived
            select new { a.RoleId, r.Code }).ToListAsync(cancellationToken);

        var roleIds = assignments.Select(x => x.RoleId).Distinct().ToList();
        var roleCodes = assignments.Select(x => x.Code).Distinct().OrderBy(x => x).ToList();
        var perms = await _db.RolePermissions.AsNoTracking()
            .Where(p => roleIds.Contains(p.RoleId) && p.Enabled)
            .ToListAsync(cancellationToken);

        HashSet<string>? ceiling = null;
        if (owner.Kind == AccessOwnerScopeKind.Seller && owner.OwnerScopeId is Guid sellerId)
        {
            ceiling = (await _db.SellerCeilings.AsNoTracking()
                .Where(c => c.SellerPartyId == sellerId && c.Enabled)
                .Select(c => c.PermissionId)
                .ToListAsync(cancellationToken)).ToHashSet(StringComparer.Ordinal);
        }

        var grouped = perms
            .GroupBy(p => (p.PermissionId, p.ScopeKind, p.ScopeResourceId))
            .Select(g =>
            {
                var def = PermissionCatalog.Find(g.Key.PermissionId);
                var denied = owner.Kind == AccessOwnerScopeKind.Seller
                    && (def?.Delegable != true || ceiling is null || !ceiling.Contains(g.Key.PermissionId));
                var via = assignments
                    .Where(a => g.Any(p => p.RoleId == a.RoleId))
                    .Select(a => a.Code)
                    .Distinct()
                    .OrderBy(x => x)
                    .ToList();
                return new EffectivePermissionDto(
                    g.Key.PermissionId,
                    def?.Module ?? "Unknown",
                    g.Key.ScopeKind,
                    g.Key.ScopeResourceId,
                    via,
                    denied);
            })
            .Where(p => !p.DeniedByCeiling)
            .OrderBy(p => p.PermissionId)
            .ToList();

        return new EffectiveAccessDto(userId, owner.Kind, owner.OwnerScopeId, grouped, roleCodes);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<AccessUserHitDto>> SearchUsersInScopeAsync(
        AccessOwnerScope owner,
        string? query,
        CancellationToken cancellationToken)
    {
        var rows = await ListAssignmentsAsync(owner, null, cancellationToken);
        var grouped = rows.GroupBy(r => r.UserId)
            .Select(g => new AccessUserHitDto(g.Key, g.Select(x => x.RoleCode).Distinct().OrderBy(x => x).ToList()))
            .ToList();
        if (!string.IsNullOrWhiteSpace(query) && Guid.TryParse(query.Trim(), out var uid))
        {
            return grouped.Where(g => g.UserId == uid).ToList();
        }

        if (!string.IsNullOrWhiteSpace(query))
        {
            var q = query.Trim();
            return grouped.Where(g => g.UserId.ToString("D").Contains(q, StringComparison.OrdinalIgnoreCase)
                || g.RoleCodes.Any(c => c.Contains(q, StringComparison.OrdinalIgnoreCase))).ToList();
        }

        return grouped;
    }

    /// <inheritdoc />
    public async Task EnsureBootstrapAsync(
        Guid? platformAdminUserId,
        IReadOnlyList<Guid> sellerPartyIds,
        string? tenantId,
        CancellationToken cancellationToken)
    {
        var platform = new AccessOwnerScope(AccessOwnerScopeKind.Platform, null, tenantId);
        var adminRole = await ScopedRoles(platform).FirstOrDefaultAsync(r => r.Code == "platform-admin", cancellationToken);
        if (adminRole is null)
        {
            adminRole = new AccessRole
            {
                Id = Guid.NewGuid(),
                TenantId = tenantId,
                OwnerScopeKind = AccessOwnerScopeKind.Platform,
                OwnerScopeId = null,
                Name = "Platform Admin",
                Code = "platform-admin",
                Description = "نقش سیستمی مدیر پلتفرم",
                IsSystem = true,
                IsMutable = false,
                IsArchived = false,
                CreatedAt = DateTimeOffset.UtcNow,
                UpdatedAt = DateTimeOffset.UtcNow,
            };
            _db.Roles.Add(adminRole);
            foreach (var perm in PermissionCatalog.All)
            {
                _db.RolePermissions.Add(new RolePermission
                {
                    Id = Guid.NewGuid(),
                    RoleId = adminRole.Id,
                    PermissionId = perm.PermissionId,
                    ScopeKind = AccessScopeKind.GlobalWithinOwner,
                    Enabled = true,
                });
            }

            await _db.SaveChangesAsync(cancellationToken);
        }

        if (platformAdminUserId is Guid adminUser)
        {
            var has = await _db.Assignments.AnyAsync(
                a => a.UserId == adminUser && a.RoleId == adminRole.Id,
                cancellationToken);
            if (!has)
            {
                _db.Assignments.Add(new UserRoleAssignment
                {
                    Id = Guid.NewGuid(),
                    UserId = adminUser,
                    RoleId = adminRole.Id,
                    OwnerScopeKind = AccessOwnerScopeKind.Platform,
                    OwnerScopeId = null,
                    AssignedAt = DateTimeOffset.UtcNow,
                });
                await _db.SaveChangesAsync(cancellationToken);
            }

            await SyncUserCapabilityTuplesAsync(adminUser, platform, cancellationToken);
        }

        foreach (var sellerId in sellerPartyIds.Distinct())
        {
            var owner = new AccessOwnerScope(AccessOwnerScopeKind.Seller, sellerId, tenantId);
            if (!await ScopedRoles(owner).AnyAsync(r => r.Code == "seller-owner", cancellationToken))
            {
                var ownerRole = new AccessRole
                {
                    Id = Guid.NewGuid(),
                    TenantId = tenantId,
                    OwnerScopeKind = AccessOwnerScopeKind.Seller,
                    OwnerScopeId = sellerId,
                    Name = "Seller Owner",
                    Code = "seller-owner",
                    Description = "نقش سیستمی مالک فروشنده",
                    IsSystem = true,
                    IsMutable = false,
                    IsArchived = false,
                    CreatedAt = DateTimeOffset.UtcNow,
                    UpdatedAt = DateTimeOffset.UtcNow,
                };
                _db.Roles.Add(ownerRole);
                foreach (var perm in PermissionCatalog.All.Where(p => p.Delegable))
                {
                    _db.RolePermissions.Add(new RolePermission
                    {
                        Id = Guid.NewGuid(),
                        RoleId = ownerRole.Id,
                        PermissionId = perm.PermissionId,
                        ScopeKind = AccessScopeKind.GlobalWithinOwner,
                        Enabled = true,
                    });
                }
            }

            if (!await _db.SellerCeilings.AnyAsync(c => c.SellerPartyId == sellerId, cancellationToken))
            {
                var now = DateTimeOffset.UtcNow;
                foreach (var perm in PermissionCatalog.All.Where(p => p.Delegable))
                {
                    _db.SellerCeilings.Add(new PlatformSellerCeiling
                    {
                        Id = Guid.NewGuid(),
                        SellerPartyId = sellerId,
                        PermissionId = perm.PermissionId,
                        Enabled = true,
                        UpdatedAt = now,
                    });
                }
            }

            await _db.SaveChangesAsync(cancellationToken);
        }
    }

    /// <inheritdoc />
    public async Task SyncUserCapabilityTuplesAsync(Guid userId, AccessOwnerScope owner, CancellationToken cancellationToken)
    {
        var effective = await GetEffectiveAccessAsync(userId, owner, cancellationToken);
        var subject = AuthorizationSubject.ForUser(userId);

        // revoke-and-rewrite for permissions we manage in this sync (catalog ids + category handlers present in effective)
        foreach (var perm in PermissionCatalog.All)
        {
            await _tuples.WriteAsync(
                new AuthorizationRelationshipWrite
                {
                    Subject = subject,
                    Relation = AuthorizationRelations.Granted,
                    Resource = new AuthorizationResource { Type = AuthorizationObjectTypes.Permission, Id = perm.PermissionId },
                    Operation = AuthorizationRelationshipOperation.Delete,
                },
                cancellationToken);
        }

        foreach (var grant in effective.Permissions)
        {
            if (grant.ScopeKind == AccessScopeKind.Category && grant.ScopeResourceId is Guid categoryId
                && (grant.PermissionId is "order.handle" or "order.view" or "order.detail" or "order.fulfill"))
            {
                await _tuples.WriteAsync(
                    new AuthorizationRelationshipWrite
                    {
                        Subject = subject,
                        Relation = AuthorizationRelations.Handler,
                        Resource = new AuthorizationResource
                        {
                            Type = AuthorizationObjectTypes.Category,
                            Id = categoryId.ToString("D"),
                        },
                        Operation = AuthorizationRelationshipOperation.Touch,
                    },
                    cancellationToken);
            }

            if (grant.ScopeKind == AccessScopeKind.GlobalWithinOwner)
            {
                await _tuples.WriteAsync(
                    new AuthorizationRelationshipWrite
                    {
                        Subject = subject,
                        Relation = AuthorizationRelations.Granted,
                        Resource = new AuthorizationResource
                        {
                            Type = AuthorizationObjectTypes.Permission,
                            Id = grant.PermissionId,
                        },
                        Operation = AuthorizationRelationshipOperation.Touch,
                    },
                    cancellationToken);
            }
        }
    }

    private IQueryable<AccessRole> ScopedRoles(AccessOwnerScope owner) =>
        _db.Roles.Where(r => r.OwnerScopeKind == owner.Kind && r.OwnerScopeId == owner.OwnerScopeId);

    private async Task<AccessRole> RequireRoleAsync(Guid roleId, AccessOwnerScope owner, CancellationToken cancellationToken)
    {
        var role = await ScopedRoles(owner).FirstOrDefaultAsync(r => r.Id == roleId, cancellationToken);
        return role ?? throw new AccessControlException("access.role.not_found", "نقش در این محدوده یافت نشد.");
    }

    private static void EnsureMutable(AccessRole role)
    {
        if (role.IsSystem || !role.IsMutable)
        {
            throw new AccessControlException("access.role.system_immutable", "نقش سیستمی یا غیرقابل‌ویرایش است.");
        }
    }

    private static void ValidateOwner(AccessOwnerScope owner)
    {
        if (owner.Kind == AccessOwnerScopeKind.Seller && owner.OwnerScopeId is null)
        {
            throw new AccessControlException("access.owner.invalid", "محدودهٔ فروشنده نامعتبر است.");
        }
    }

    private async Task ValidateGrantsForOwnerAsync(
        AccessOwnerScope owner,
        IReadOnlyList<RolePermissionGrant> grants,
        CancellationToken cancellationToken)
    {
        HashSet<string>? ceiling = null;
        if (owner.Kind == AccessOwnerScopeKind.Seller && owner.OwnerScopeId is Guid sellerId)
        {
            ceiling = (await _db.SellerCeilings.AsNoTracking()
                .Where(c => c.SellerPartyId == sellerId && c.Enabled)
                .Select(c => c.PermissionId)
                .ToListAsync(cancellationToken)).ToHashSet(StringComparer.Ordinal);
        }

        foreach (var grant in grants.Where(g => g.Enabled))
        {
            var def = PermissionCatalog.Require(grant.PermissionId);
            if (!def.ScopeKinds.Contains(grant.ScopeKind))
            {
                throw new AccessControlException("access.scope.unsupported", $"Scope برای {grant.PermissionId} مجاز نیست.");
            }

            if (owner.Kind == AccessOwnerScopeKind.Seller)
            {
                if (!def.Delegable)
                {
                    throw new AccessControlException("access.escalation.platform_permission", $"فروشنده نمی‌تواند مجوز پلتفرم بدهد: {grant.PermissionId}");
                }

                if (ceiling is null || !ceiling.Contains(grant.PermissionId))
                {
                    throw new AccessControlException("access.escalation.ceiling", $"مجوز خارج از سقف پلتفرم است: {grant.PermissionId}");
                }
            }
        }
    }

    private async Task<IReadOnlyList<AccessRoleDto>> MapRolesAsync(IReadOnlyList<AccessRole> roles, CancellationToken cancellationToken)
    {
        var ids = roles.Select(r => r.Id).ToList();
        var permCounts = await _db.RolePermissions.AsNoTracking()
            .Where(p => ids.Contains(p.RoleId) && p.Enabled)
            .GroupBy(p => p.RoleId)
            .Select(g => new { RoleId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.RoleId, x => x.Count, cancellationToken);
        var assignCounts = await _db.Assignments.AsNoTracking()
            .Where(a => ids.Contains(a.RoleId))
            .GroupBy(a => a.RoleId)
            .Select(g => new { RoleId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.RoleId, x => x.Count, cancellationToken);

        return roles.Select(r => new AccessRoleDto(
            r.Id,
            r.OwnerScopeKind,
            r.OwnerScopeId,
            r.Name,
            r.Code,
            r.Description,
            r.IsSystem,
            r.IsMutable,
            r.IsArchived,
            permCounts.GetValueOrDefault(r.Id),
            assignCounts.GetValueOrDefault(r.Id),
            r.CreatedAt,
            r.UpdatedAt)).ToList();
    }

    private async Task AuditAsync(
        Guid actorUserId,
        string action,
        string targetType,
        string targetId,
        Guid? sellerScopeId,
        string before,
        string after,
        string? traceId,
        CancellationToken cancellationToken)
    {
        _db.AuditEvents.Add(new AccessAuditEvent
        {
            Id = Guid.NewGuid(),
            ActorUserId = actorUserId,
            Action = action,
            TargetType = targetType,
            TargetId = targetId,
            SellerScopeId = sellerScopeId,
            BeforeSummary = Truncate(before, 1024),
            AfterSummary = Truncate(after, 1024),
            TraceId = traceId ?? string.Empty,
            At = DateTimeOffset.UtcNow,
        });
        await Task.CompletedTask;
    }

    private static string RequireText(string value, int max)
    {
        var trimmed = (value ?? string.Empty).Trim();
        if (trimmed.Length == 0 || trimmed.Length > max)
        {
            throw new AccessControlException("access.validation.text", "متن نامعتبر است.");
        }

        return trimmed;
    }

    private static string RequireCode(string value)
    {
        var trimmed = (value ?? string.Empty).Trim().ToLowerInvariant();
        if (trimmed.Length is < 2 or > 64 || trimmed.Any(c => !(char.IsLetterOrDigit(c) || c is '-' or '_')))
        {
            throw new AccessControlException("access.validation.code", "کد نقش نامعتبر است.");
        }

        return trimmed;
    }

    private static string Truncate(string value, int max) =>
        value.Length <= max ? value : value[..max];
}
