using MediatR;
using Tooba.AccessControl.Domain;

namespace Tooba.AccessControl.Application.Commands.UpdateRole;

/// <summary>
/// فرمان به‌روزرسانی نقش در محدودهٔ مالک مشخص.
/// </summary>
/// <param name="RoleId">شناسهٔ نقش.</param>
/// <param name="OwnerScopeKind">گونهٔ محدودهٔ مالک.</param>
/// <param name="OwnerScopeId">شناسهٔ مالک در محدودهٔ Seller.</param>
/// <param name="ActorUserId">شناسهٔ Actor مجاز (از لایهٔ مجوز).</param>
/// <param name="TenantId">شناسهٔ Tenant جاری در صورت وجود.</param>
/// <param name="Name">نام نقش.</param>
/// <param name="Description">توضیح.</param>
/// <param name="TraceId">شناسهٔ رهگیری درخواست.</param>
public sealed record UpdateRoleCommand(
    Guid RoleId,
    AccessOwnerScopeKind OwnerScopeKind,
    Guid? OwnerScopeId,
    Guid ActorUserId,
    string? TenantId,
    string Name,
    string Description,
    string? TraceId) : IRequest<AccessRoleDto>;

/// <summary>Handler فرمان به‌روزرسانی نقش.</summary>
public sealed class UpdateRoleCommandHandler : IRequestHandler<UpdateRoleCommand, AccessRoleDto>
{
    private readonly IAccessControlDirectory _directory;

    /// <summary>سازنده.</summary>
    /// <param name="directory">دایرکتوری دسترسی.</param>
    public UpdateRoleCommandHandler(IAccessControlDirectory directory) => _directory = directory;

    /// <inheritdoc />
    public Task<AccessRoleDto> Handle(UpdateRoleCommand request, CancellationToken cancellationToken)
    {
        var owner = new AccessOwnerScope(request.OwnerScopeKind, request.OwnerScopeId, request.TenantId);
        return _directory.UpdateRoleAsync(
            request.RoleId,
            owner,
            new UpdateAccessRoleCommand(request.Name, request.Description),
            request.ActorUserId,
            request.TraceId,
            cancellationToken);
    }
}
