using MediatR;
using Tooba.AccessControl.Domain;

namespace Tooba.AccessControl.Application.Commands.CreateRole;

/// <summary>
/// فرمان ایجاد نقش در محدودهٔ پلتفرم.
/// </summary>
/// <param name="ActorUserId">شناسهٔ Actor مجاز (از لایهٔ مجوز).</param>
/// <param name="TenantId">شناسهٔ Tenant جاری در صورت وجود.</param>
/// <param name="Name">نام نقش.</param>
/// <param name="Code">کد نقش.</param>
/// <param name="Description">توضیح.</param>
/// <param name="TraceId">شناسهٔ رهگیری درخواست.</param>
public sealed record CreateRoleCommand(
    Guid ActorUserId,
    string? TenantId,
    string Name,
    string Code,
    string Description,
    string? TraceId) : IRequest<AccessRoleDto>;

/// <summary>Handler فرمان ایجاد نقش.</summary>
public sealed class CreateRoleCommandHandler : IRequestHandler<CreateRoleCommand, AccessRoleDto>
{
    private readonly IAccessControlDirectory _directory;

    /// <summary>سازنده.</summary>
    /// <param name="directory">دایرکتوری دسترسی.</param>
    public CreateRoleCommandHandler(IAccessControlDirectory directory) => _directory = directory;

    /// <inheritdoc />
    public Task<AccessRoleDto> Handle(CreateRoleCommand request, CancellationToken cancellationToken)
    {
        var owner = new AccessOwnerScope(AccessOwnerScopeKind.Platform, null, request.TenantId);
        return _directory.CreateRoleAsync(
            owner,
            new CreateAccessRoleCommand(request.Name, request.Code, request.Description),
            request.ActorUserId,
            request.TraceId,
            cancellationToken);
    }
}
