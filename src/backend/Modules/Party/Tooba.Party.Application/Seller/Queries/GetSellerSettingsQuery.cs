using MediatR;
using Tooba.BuildingBlocks;
using Tooba.BuildingBlocks.Results;
using Tooba.Party.Application.Seller.Models;
using Tooba.Party.Contracts;

namespace Tooba.Party.Application.Seller.Queries;

/// <summary>
/// پروفایل عملیاتی Organization فروشنده را با پرچم قابلیت مدیریت می‌خواند.
/// SellerPartyId از مرز مجوز فروشنده می‌آید، نه از بدنهٔ درخواست.
/// </summary>
public sealed record GetSellerSettingsQuery(Guid SellerPartyId, bool CanManage)
    : IRequest<Result<PartySellerSettingsView>>;

/// <summary>Handler خواندن تنظیمات فروشنده روی درز پایدار Party.</summary>
public sealed class GetSellerSettingsQueryHandler(IPartySellerSettings settings)
    : IRequestHandler<GetSellerSettingsQuery, Result<PartySellerSettingsView>>
{
    /// <inheritdoc />
    public async Task<Result<PartySellerSettingsView>> Handle(
        GetSellerSettingsQuery request,
        CancellationToken cancellationToken)
    {
        var snapshot = await settings.GetAsync(request.SellerPartyId, cancellationToken);
        if (snapshot is null)
        {
            return Result.Failure<PartySellerSettingsView>(
                new SemanticError(PartySellerSettingsErrorCodes.Missing));
        }

        return Result.Success(new PartySellerSettingsView(
            snapshot.PartyId,
            snapshot.DisplayName,
            snapshot.LegalName,
            snapshot.Description,
            snapshot.SupportPhone,
            snapshot.SupportEmail,
            snapshot.AddressLine,
            snapshot.UpdatedAt,
            request.CanManage));
    }
}
