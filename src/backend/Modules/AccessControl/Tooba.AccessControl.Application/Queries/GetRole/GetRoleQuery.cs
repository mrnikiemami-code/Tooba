using MediatR;
using Tooba.BuildingBlocks.Results;
using Tooba.AccessControl.Application.Composition;
using Tooba.AccessControl.Contracts.Errors;
using Tooba.AccessControl.Domain;
using Tooba.AccessControl.Application.Models;
using Tooba.AccessControl.Application.Permissions;
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
    string? TenantId) : IRequest<Result<AccessRoleDto>>;

/// <summary>Handler پرس‌وجوی یک نقش.</summary>
public sealed class GetRoleQueryHandler : IRequestHandler<GetRoleQuery, Result<AccessRoleDto>>
{
    private readonly IAccessControlDirectory _directory;

    /// <summary>سازنده.</summary>
    /// <param name="directory">دایرکتوری دسترسی.</param>
    public GetRoleQueryHandler(IAccessControlDirectory directory) => _directory = directory;

    /// <inheritdoc />
    public async Task<Result<AccessRoleDto>> Handle(GetRoleQuery request, CancellationToken cancellationToken)
    {
        var owner = new AccessOwnerScope(request.OwnerScopeKind, request.OwnerScopeId, request.TenantId);
        var wrapped = await AccessControlOperation.ExecuteAsync(
            () => _directory.GetRoleAsync(request.RoleId, owner, cancellationToken));
        if (wrapped.IsFailure)
            return Result.Failure<AccessRoleDto>(wrapped.Errors);
        return AccessControlOperation.NotFoundIfNull(wrapped.Value, AccessControlErrorCodes.RoleNotFound);
    }
}