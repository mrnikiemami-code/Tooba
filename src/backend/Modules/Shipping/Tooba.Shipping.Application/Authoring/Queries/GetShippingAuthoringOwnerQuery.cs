using MediatR;
using Tooba.BuildingBlocks;
using Tooba.BuildingBlocks.Results;
using Tooba.Shipping.Application.Authoring.Models;
using Tooba.Shipping.Contracts.Errors;

namespace Tooba.Shipping.Application.Authoring.Queries;

/// <summary>
/// خواندن مالکِ مجازشدهٔ قواعد ارسال برای همین درخواست. مسیر ادمین و مسیر فروشنده هر دو همین یک
/// use case را صدا می‌زنند؛ تفاوتشان فقط در مجوزدهی سرور و شکل scope است.
/// </summary>
/// <param name="Scope">
/// محدودهٔ مالک که <b>سمت سرور</b> پس از مجوزدهی ساخته است. هیچ route/query/body آن را bind نمی‌کند،
/// پس ورودی فراخوان‌کننده‌ای وجود ندارد که مالک/حالت/Tenant/فروشنده را جعل کند.
/// </param>
public sealed record GetShippingAuthoringOwnerQuery(ShippingAuthoringOwnerScope Scope)
    : IRequest<Result<ShippingAuthoringOwnerView>>;

/// <summary>
/// هندلر فقط-خواندنی مالک نویسندگی. هیچ HttpContext یا ماژول خارجی را نمی‌خواند و هیچ ردیف
/// owner-scoped را فهرست/ذخیره/فعال نمی‌کند؛ فقط scope مجازشده را به نمای فشرده تبدیل می‌کند.
/// </summary>
public sealed class GetShippingAuthoringOwnerHandler()
    : IRequestHandler<GetShippingAuthoringOwnerQuery, Result<ShippingAuthoringOwnerView>>
{
    /// <inheritdoc />
    public Task<Result<ShippingAuthoringOwnerView>> Handle(
        GetShippingAuthoringOwnerQuery request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        // scope همیشه از کارخانه‌های خودش ساخته می‌شود؛ نبودش خطای برنامه‌نویسی است، نه ورودی کاربر،
        // و نباید به یک مالک پیش‌فرض یا پاسخ موفق جعلی تبدیل شود.
        ArgumentNullException.ThrowIfNull(request.Scope);
        cancellationToken.ThrowIfCancellationRequested();

        return Task.FromResult(request.Scope.Kind switch
        {
            ShippingAuthoringOwnerKind.Store => Result.Success(
                new ShippingAuthoringOwnerView("Store", SellerPartyId: null)),
            ShippingAuthoringOwnerKind.Marketplace => Result.Success(
                new ShippingAuthoringOwnerView("Marketplace", SellerPartyId: null)),
            ShippingAuthoringOwnerKind.Seller => Result.Success(
                new ShippingAuthoringOwnerView("Seller", request.Scope.SellerPartyId)),
            _ => Result.Failure<ShippingAuthoringOwnerView>(
                new SemanticError(ShippingErrorCodes.AuthoringOwnerScopeUnavailable)),
        });
    }
}
