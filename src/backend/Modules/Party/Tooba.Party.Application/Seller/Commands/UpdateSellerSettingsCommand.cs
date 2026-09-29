using MediatR;
using Tooba.BuildingBlocks;
using Tooba.BuildingBlocks.Results;
using Tooba.Party.Application.Seller.Models;
using Tooba.Party.Contracts;

namespace Tooba.Party.Application.Seller.Commands;

/// <summary>
/// پروفایل عملیاتی Organization فروشنده را به‌روزرسانی می‌کند.
/// <para>
/// مرز مجوز (<c>seller.settings.manage</c>) در Endpoint بررسی می‌شود و این فرمان همهٔ شاخه‌های
/// شکست کسب‌وکار را به <c>seller.settings.rejected</c> نگاشت می‌کند — همان قرارداد قبلی Host.
/// </para>
/// </summary>
public sealed record UpdateSellerSettingsCommand(
    Guid SellerPartyId,
    PartySellerSettingsWriteModel Input)
    : IRequest<Result<PartySellerSettingsView>>;

/// <summary>Handler نوشتن تنظیمات فروشنده روی درز پایدار Party.</summary>
public sealed class UpdateSellerSettingsCommandHandler(IPartySellerSettings settings)
    : IRequestHandler<UpdateSellerSettingsCommand, Result<PartySellerSettingsView>>
{
    /// <inheritdoc />
    public async Task<Result<PartySellerSettingsView>> Handle(
        UpdateSellerSettingsCommand request,
        CancellationToken cancellationToken)
    {
        try
        {
            var updated = await settings.UpdateAsync(
                request.SellerPartyId,
                new PartySellerSettingsWrite(
                    request.Input.DisplayName,
                    request.Input.LegalName,
                    request.Input.Description,
                    request.Input.SupportPhone,
                    request.Input.SupportEmail,
                    request.Input.AddressLine),
                cancellationToken);

            return Result.Success(new PartySellerSettingsView(
                updated.PartyId,
                updated.DisplayName,
                updated.LegalName,
                updated.Description,
                updated.SupportPhone,
                updated.SupportEmail,
                updated.AddressLine,
                updated.UpdatedAt,
                CanManage: true));
        }
        catch (InvalidOperationException)
        {
            // Domain reject (Person مقصد یا مرز اعتبارسنجی دامنه) — همان معنای ۴۰۰ قرارداد قبلی.
            return Result.Failure<PartySellerSettingsView>(
                new SemanticError(PartySellerSettingsErrorCodes.Rejected));
        }
    }
}
