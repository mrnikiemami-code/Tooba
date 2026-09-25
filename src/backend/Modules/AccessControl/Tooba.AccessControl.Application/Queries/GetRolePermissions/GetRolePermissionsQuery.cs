using MediatR;
using Tooba.AccessControl.Domain;

using Tooba.AccessControl.Application.Models;
using Tooba.AccessControl.Application.Permissions;
namespace Tooba.AccessControl.Application.Queries.GetRolePermissions;

/// <summary>
/// پرس‌وجوی مجوزهای یک نقش در محدودهٔ مالک مشخص.
/// </summary>
/// <param name="RoleId">شناسهٔ نقش.</param>
/// <param name="OwnerScopeKind">گونهٔ محدودهٔ مالک.</param>
/// <param name="OwnerScopeId">شناسهٔ مالک در محدودهٔ Seller.</param>
/// <param name="TenantId">شناسهٔ Tenant جاری در صورت وجود.</param>
public sealed record GetRolePermissionsQuery(
    Guid RoleId,
    AccessOwnerScopeKind OwnerScopeKind,
    Guid? OwnerScopeId,
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
        var owner = new AccessOwnerScope(request.OwnerScopeKind, request.OwnerScopeId, request.TenantId);
        return _directory.GetRolePermissionsAsync(request.RoleId, owner, cancellationToken);
    }
}
