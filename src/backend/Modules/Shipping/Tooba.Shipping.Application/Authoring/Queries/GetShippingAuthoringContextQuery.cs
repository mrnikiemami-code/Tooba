using MediatR;
using Tooba.BuildingBlocks;
using Tooba.BuildingBlocks.Results;
using Tooba.Shipping.Application.Authoring.Models;
using Tooba.Shipping.Application.Authoring.Ports;
using Tooba.Shipping.Contracts.Errors;

namespace Tooba.Shipping.Application.Authoring.Queries;

/// <summary>
/// خواندن زمینهٔ نویسندگی ادمین ارسال. این query هیچ ورودی فراخوان‌کننده‌ای ندارد: حالت نصب، ارز و
/// زبان‌ها همگی از زمینه/منابع authoritative سرور می‌آیند، پس هیچ override مالك/حالت/زبان/ارز از
/// route، query string یا body پذیرفته نمی‌شود.
/// </summary>
public sealed record GetShippingAuthoringContextQuery()
    : IRequest<Result<ShippingAuthoringContextView>>;

/// <summary>
/// هندلر فقط-خواندنی زمینهٔ نویسندگی. کارش projection آماده‌سازی مصرف‌کننده است: انتخاب زبان‌های
/// فعال، اعتبارسنجی readiness و ساخت نمای typed. این هندلر هیچ زبان/ارز/حالتی را فعال یا اصلاح
/// نمی‌کند و سیاست پیش‌فرض Localization را تکرار نمی‌کند.
/// </summary>
public sealed class GetShippingAuthoringContextHandler(IShippingAuthoringContextSource source)
    : IRequestHandler<GetShippingAuthoringContextQuery, Result<ShippingAuthoringContextView>>
{
    /// <inheritdoc />
    public async Task<Result<ShippingAuthoringContextView>> Handle(
        GetShippingAuthoringContextQuery request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        // استثنای وابستگی/لغو عمداً گرفته نمی‌شود: از مرز canonical موجود عبور می‌کند و هرگز به
        // موفقیت ساختگی یا context خالی تبدیل نمی‌شود.
        var snapshot = await source.ReadAsync(cancellationToken);

        // TB-192: همان projection TB-188 اکنون در یک helper مشترک است تا capability ارائهٔ چندزبانه
        // الگوریتم readiness را تکرار نکند. رفتار/کد خطا/ترتیب این query تغییر نکرده است.
        return ShippingAuthoringContextProjection.Project(snapshot);
    }
}
