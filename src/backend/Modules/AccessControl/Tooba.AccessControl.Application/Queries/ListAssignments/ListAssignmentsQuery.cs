using MediatR;
using Tooba.AccessControl.Domain;

using Tooba.AccessControl.Application.Models;
using Tooba.AccessControl.Application.Permissions;
namespace Tooba.AccessControl.Application.Queries.ListAssignments;

/// <summary>
/// پرس‌وجوی فهرست تخصیص‌های نقش در محدودهٔ مالک مشخص.
/// </summary>
/// <param name="OwnerScopeKind">گونهٔ محدودهٔ مالک.</param>
/// <param name="OwnerScopeId">شناسهٔ مالک در محدودهٔ Seller.</param>
/// <param name="TenantId">شناسهٔ Tenant جاری در صورت وجود.</param>
/// <param name="UserId">فیلتر اختیاری کاربر.</param>
public sealed record ListAssignmentsQuery(
    AccessOwnerScopeKind OwnerScopeKind,
    Guid? OwnerScopeId,
    string? TenantId,
    Guid? UserId) : IRequest<IReadOnlyList<UserRoleAssignmentDto>>;

/// <summary>Handler پرس‌وجوی فهرست تخصیص‌ها.</summary>
public sealed class ListAssignmentsQueryHandler
    : IRequestHandler<ListAssignmentsQuery, IReadOnlyList<UserRoleAssignmentDto>>
{
    private readonly IAccessControlDirectory _directory;

    /// <summary>سازنده.</summary>
    /// <param name="directory">دایرکتوری دسترسی.</param>
    public ListAssignmentsQueryHandler(IAccessControlDirectory directory) => _directory = directory;

    /// <inheritdoc />
    public Task<IReadOnlyList<UserRoleAssignmentDto>> Handle(
        ListAssignmentsQuery request, CancellationToken cancellationToken)
    {
        var owner = new AccessOwnerScope(request.OwnerScopeKind, request.OwnerScopeId, request.TenantId);
        return _directory.ListAssignmentsAsync(owner, request.UserId, cancellationToken);
    }
}
