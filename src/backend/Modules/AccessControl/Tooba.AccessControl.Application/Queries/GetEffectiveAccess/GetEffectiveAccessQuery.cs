using MediatR;
using Tooba.BuildingBlocks.Results;
using Tooba.AccessControl.Application.Composition;
using Tooba.AccessControl.Contracts.Enums;
using Tooba.AccessControl.Application.Models;
using Tooba.AccessControl.Application.Ports;
using Tooba.AccessControl.Application.Permissions;

namespace Tooba.AccessControl.Application.Queries.GetEffectiveAccess;

/// <summary>
/// پرس‌وجوی مجوز مؤثر یک Actor در محدودهٔ مالک مشخص.
/// </summary>
/// <param name="ActorUserId">شناسهٔ Actor (از لایهٔ مجوز/زمینه، معتبر).</param>
/// <param name="OwnerScopeKind">گونهٔ مالک.</param>
/// <param name="OwnerScopeId">شناسهٔ مالک در محدودهٔ Seller.</param>
/// <param name="TenantId">شناسهٔ Tenant جاری در صورت وجود.</param>
public sealed record GetEffectiveAccessQuery(
    Guid ActorUserId,
    AccessOwnerScopeKind OwnerScopeKind,
    Guid? OwnerScopeId,
    string? TenantId) : IRequest<Result<EffectiveAccessDto>>;

/// <summary>Handler پرس‌وجوی مجوز مؤثر.</summary>
public sealed class GetEffectiveAccessQueryHandler : IRequestHandler<GetEffectiveAccessQuery, Result<EffectiveAccessDto>>
{
    private readonly IAccessControlDirectory _directory;

    /// <summary>سازنده.</summary>
    /// <param name="directory">دایرکتوری دسترسی.</param>
    public GetEffectiveAccessQueryHandler(IAccessControlDirectory directory) => _directory = directory;

    /// <inheritdoc />
    public Task<Result<EffectiveAccessDto>> Handle(GetEffectiveAccessQuery request, CancellationToken cancellationToken)
    {
        var owner = new AccessOwnerScope(request.OwnerScopeKind, request.OwnerScopeId, request.TenantId);
        return AccessControlOperation.ExecuteAsync(() =>
            _directory.GetEffectiveAccessAsync(request.ActorUserId, owner, cancellationToken));
    }
}
