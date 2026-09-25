using MediatR;
using Tooba.AccessControl.Domain;

using Tooba.AccessControl.Application.Models;
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
    string? TenantId) : IRequest<EffectiveAccessDto>;

/// <summary>Handler پرس‌وجوی مجوز مؤثر.</summary>
public sealed class GetEffectiveAccessQueryHandler : IRequestHandler<GetEffectiveAccessQuery, EffectiveAccessDto>
{
    private readonly IAccessControlDirectory _directory;

    /// <summary>سازنده.</summary>
    /// <param name="directory">دایرکتوری دسترسی.</param>
    public GetEffectiveAccessQueryHandler(IAccessControlDirectory directory) => _directory = directory;

    /// <inheritdoc />
    public Task<EffectiveAccessDto> Handle(GetEffectiveAccessQuery request, CancellationToken cancellationToken)
    {
        var owner = new AccessOwnerScope(request.OwnerScopeKind, request.OwnerScopeId, request.TenantId);
        return _directory.GetEffectiveAccessAsync(request.ActorUserId, owner, cancellationToken);
    }
}
