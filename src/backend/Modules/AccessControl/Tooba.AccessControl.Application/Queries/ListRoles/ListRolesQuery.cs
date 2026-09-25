using MediatR;
using Tooba.AccessControl.Domain;

using Tooba.AccessControl.Application.Models;
using Tooba.AccessControl.Application.Permissions;
namespace Tooba.AccessControl.Application.Queries.ListRoles;

/// <summary>
/// پرس‌وجوی فهرست نقش‌های یک محدودهٔ مالک.
/// </summary>
/// <param name="OwnerScopeKind">گونهٔ مالک.</param>
/// <param name="OwnerScopeId">شناسهٔ مالک در محدودهٔ Seller.</param>
/// <param name="TenantId">شناسهٔ Tenant جاری در صورت وجود.</param>
/// <param name="IncludeArchived">شامل نقش‌های بایگانی‌شده.</param>
public sealed record ListRolesQuery(
    AccessOwnerScopeKind OwnerScopeKind,
    Guid? OwnerScopeId,
    string? TenantId,
    bool IncludeArchived) : IRequest<IReadOnlyList<AccessRoleDto>>;

/// <summary>Handler پرس‌وجوی فهرست نقش‌ها.</summary>
public sealed class ListRolesQueryHandler : IRequestHandler<ListRolesQuery, IReadOnlyList<AccessRoleDto>>
{
    private readonly IAccessControlDirectory _directory;

    /// <summary>سازنده.</summary>
    /// <param name="directory">دایرکتوری دسترسی.</param>
    public ListRolesQueryHandler(IAccessControlDirectory directory) => _directory = directory;

    /// <inheritdoc />
    public Task<IReadOnlyList<AccessRoleDto>> Handle(ListRolesQuery request, CancellationToken cancellationToken)
    {
        var owner = new AccessOwnerScope(request.OwnerScopeKind, request.OwnerScopeId, request.TenantId);
        return _directory.ListRolesAsync(owner, request.IncludeArchived, cancellationToken);
    }
}
