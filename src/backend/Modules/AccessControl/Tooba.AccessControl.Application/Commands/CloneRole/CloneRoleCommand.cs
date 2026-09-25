using MediatR;
using Tooba.AccessControl.Domain;

namespace Tooba.AccessControl.Application.Commands.CloneRole;

/// <summary>
/// فرمان کلون نقش در محدودهٔ پلتفرم.
/// </summary>
/// <param name="RoleId">شناسهٔ نقش مبدأ.</param>
/// <param name="ActorUserId">شناسهٔ Actor مجاز (از لایهٔ مجوز).</param>
/// <param name="TenantId">شناسهٔ Tenant جاری در صورت وجود.</param>
/// <param name="Name">نام نقش جدید.</param>
/// <param name="Code">کد نقش جدید.</param>
/// <param name="Description">توضیح نقش جدید (در صورت نبود، از مبدأ کپی می‌شود).</param>
/// <param name="TraceId">شناسهٔ رهگیری درخواست.</param>
public sealed record CloneRoleCommand(
    Guid RoleId,
    Guid ActorUserId,
    string? TenantId,
    string Name,
    string Code,
    string? Description,
    string? TraceId) : IRequest<AccessRoleDto>;

/// <summary>Handler فرمان کلون نقش.</summary>
public sealed class CloneRoleCommandHandler : IRequestHandler<CloneRoleCommand, AccessRoleDto>
{
    private readonly IAccessControlDirectory _directory;

    /// <summary>سازنده.</summary>
    /// <param name="directory">دایرکتوری دسترسی.</param>
    public CloneRoleCommandHandler(IAccessControlDirectory directory) => _directory = directory;

    /// <inheritdoc />
    public Task<AccessRoleDto> Handle(CloneRoleCommand request, CancellationToken cancellationToken)
    {
        var owner = new AccessOwnerScope(AccessOwnerScopeKind.Platform, null, request.TenantId);
        return _directory.CloneRoleAsync(
            request.RoleId,
            owner,
            new CloneAccessRoleCommand(request.Name, request.Code, request.Description),
            request.ActorUserId,
            request.TraceId,
            cancellationToken);
    }
}
