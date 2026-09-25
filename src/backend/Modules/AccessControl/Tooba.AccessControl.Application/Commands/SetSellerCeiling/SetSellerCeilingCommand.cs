using MediatR;
using Tooba.AccessControl.Domain;

using Tooba.AccessControl.Application.Models;
using Tooba.AccessControl.Application.Permissions;
namespace Tooba.AccessControl.Application.Commands.SetSellerCeiling;

/// <summary>
/// ورودی یک ردیف سقف مجوز فروشنده در لایهٔ Application.
/// </summary>
/// <param name="PermissionId">شناسهٔ مجوز.</param>
/// <param name="Enabled">فعال بودن.</param>
/// <param name="ScopeKind">گونهٔ محدوده.</param>
/// <param name="ScopeResourceId">شناسهٔ منبع محدوده در صورت وجود.</param>
public sealed record SellerCeilingEntryInput(
    string PermissionId,
    bool Enabled,
    AccessScopeKind ScopeKind = AccessScopeKind.GlobalWithinOwner,
    Guid? ScopeResourceId = null);

/// <summary>
/// فرمان تنظیم سقف مجوزهای یک فروشنده.
/// </summary>
/// <param name="SellerPartyId">شناسهٔ فروشنده.</param>
/// <param name="Entries">ردیف‌های سقف.</param>
/// <param name="ActorUserId">شناسهٔ Actor مجاز (از لایهٔ مجوز).</param>
/// <param name="TraceId">شناسهٔ رهگیری درخواست.</param>
public sealed record SetSellerCeilingCommand(
    Guid SellerPartyId,
    IReadOnlyList<SellerCeilingEntryInput> Entries,
    Guid ActorUserId,
    string? TraceId) : IRequest<Unit>;

/// <summary>Handler فرمان تنظیم سقف فروشنده.</summary>
public sealed class SetSellerCeilingCommandHandler : IRequestHandler<SetSellerCeilingCommand, Unit>
{
    private readonly IAccessControlDirectory _directory;

    /// <summary>سازنده.</summary>
    /// <param name="directory">دایرکتوری دسترسی.</param>
    public SetSellerCeilingCommandHandler(IAccessControlDirectory directory) => _directory = directory;

    /// <inheritdoc />
    public async Task<Unit> Handle(SetSellerCeilingCommand request, CancellationToken cancellationToken)
    {
        var entries = request.Entries
            .Select(e => (e.PermissionId, e.Enabled, e.ScopeKind, e.ScopeResourceId))
            .ToList();

        await _directory.SetSellerCeilingAsync(
            request.SellerPartyId,
            entries,
            request.ActorUserId,
            request.TraceId,
            cancellationToken);
        return Unit.Value;
    }
}
