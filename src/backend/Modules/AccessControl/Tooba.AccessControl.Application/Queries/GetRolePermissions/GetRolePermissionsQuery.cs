using MediatR;
using Tooba.AccessControl.Domain;

namespace Tooba.AccessControl.Application.Queries.GetRolePermissions;

/// <summary>
/// پرس‌وجوی مجوزهای یک نقش در محدودهٔ پلتفرم.
/// </summary>
/// <param name="RoleId">شناسهٔ نقش.</param>
/// <param name="TenantId">شناسهٔ Tenant جاری در صورت وجود.</param>
public sealed record GetRolePermissionsQuery(
    Guid RoleId,
    string? TenantId) : IRequest<IReadOnlyList<RolePermissionGrant>>;

/// <summary>Handler پرس‌وجوی مجوزهای نقش.</summary>
public sealed class GetRolePermissionsQueryHandler
    : IRequestHandler<GetRolePermissionsQuery, IReadOnlyList<RolePermissionGrant>>
{
    private readonly IAccessControlDirectory _directory;

    /// <summary>سازنده.</summary>
    /// <param name="directory">دایرکتوری دسترسی.</param>
    public GetRolePermissionsQueryHandler(IAccessControlDirectory directory) => _directory = directory;

    /// <inheritdoc />
    public Task<IReadOnlyList<RolePermissionGrant>> Handle(
        GetRolePermissionsQuery request, CancellationToken cancellationToken)
    {
        var owner = new AccessOwnerScope(AccessOwnerScopeKind.Platform, null, request.TenantId);
        return _directory.GetRolePermissionsAsync(request.RoleId, owner, cancellationToken);
    }
}
