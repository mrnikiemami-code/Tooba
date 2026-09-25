using MediatR;
using Tooba.AccessControl.Domain;

namespace Tooba.AccessControl.Application.Queries.GetRole;

/// <summary>
/// پرس‌وجوی یک نقش در محدودهٔ مالک مشخص.
/// </summary>
/// <param name="RoleId">شناسهٔ نقش.</param>
/// <param name="OwnerScopeKind">گونهٔ مالک.</param>
/// <param name="OwnerScopeId">شناسهٔ مالک در محدودهٔ Seller.</param>
/// <param name="TenantId">شناسهٔ Tenant جاری در صورت وجود.</param>
public sealed record GetRoleQuery(
    Guid RoleId,
    AccessOwnerScopeKind OwnerScopeKind,
    Guid? OwnerScopeId,
    string? TenantId) : IRequest<AccessRoleDto?>;

/// <summary>Handler پرس‌وجوی یک نقش.</summary>
public sealed class GetRoleQueryHandler : IRequestHandler<GetRoleQuery, AccessRoleDto?>
{
    private readonly IAccessControlDirectory _directory;

    /// <summary>سازنده.</summary>
    /// <param name="directory">دایرکتوری دسترسی.</param>
    public GetRoleQueryHandler(IAccessControlDirectory directory) => _directory = directory;

    /// <inheritdoc />
    public Task<AccessRoleDto?> Handle(GetRoleQuery request, CancellationToken cancellationToken)
    {
        var owner = new AccessOwnerScope(request.OwnerScopeKind, request.OwnerScopeId, request.TenantId);
        return _directory.GetRoleAsync(request.RoleId, owner, cancellationToken);
    }
}
