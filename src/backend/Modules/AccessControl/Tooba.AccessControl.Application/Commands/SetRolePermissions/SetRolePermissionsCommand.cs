using MediatR;
using Tooba.AccessControl.Domain;

namespace Tooba.AccessControl.Application.Commands.SetRolePermissions;

/// <summary>
/// فرمان جایگزینی مجوزهای یک نقش در محدودهٔ مالک مشخص.
/// </summary>
/// <param name="RoleId">شناسهٔ نقش.</param>
/// <param name="OwnerScopeKind">گونهٔ محدودهٔ مالک.</param>
/// <param name="OwnerScopeId">شناسهٔ مالک در محدودهٔ Seller.</param>
/// <param name="ActorUserId">شناسهٔ Actor مجاز (از لایهٔ مجوز).</param>
/// <param name="TenantId">شناسهٔ Tenant جاری در صورت وجود.</param>
/// <param name="Grants">فهرست اعطاهای مجوز.</param>
/// <param name="TraceId">شناسهٔ رهگیری درخواست.</param>
public sealed record SetRolePermissionsCommand(
    Guid RoleId,
    AccessOwnerScopeKind OwnerScopeKind,
    Guid? OwnerScopeId,
    Guid ActorUserId,
    string? TenantId,
    IReadOnlyList<RolePermissionGrant> Grants,
    string? TraceId) : IRequest<Unit>;

/// <summary>Handler فرمان جایگزینی مجوزهای نقش.</summary>
public sealed class SetRolePermissionsCommandHandler : IRequestHandler<SetRolePermissionsCommand, Unit>
{
    private readonly IAccessControlDirectory _directory;

    /// <summary>سازنده.</summary>
    /// <param name="directory">دایرکتوری دسترسی.</param>
    public SetRolePermissionsCommandHandler(IAccessControlDirectory directory) => _directory = directory;

    /// <inheritdoc />
    public async Task<Unit> Handle(SetRolePermissionsCommand request, CancellationToken cancellationToken)
    {
        var owner = new AccessOwnerScope(request.OwnerScopeKind, request.OwnerScopeId, request.TenantId);
        await _directory.SetRolePermissionsAsync(
            request.RoleId,
            owner,
            request.Grants,
            request.ActorUserId,
            request.TraceId,
            cancellationToken);
        return Unit.Value;
    }
}
