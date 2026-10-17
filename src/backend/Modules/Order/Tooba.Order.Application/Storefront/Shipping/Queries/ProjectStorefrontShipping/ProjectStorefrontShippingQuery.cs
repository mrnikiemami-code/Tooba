using MediatR;
using Tooba.BuildingBlocks.Results;
using Tooba.Geography.Contracts.Places;
using Tooba.Order.Application.Storefront.Models;
using Tooba.Order.Application.Storefront.Services;
using Tooba.Order.Application.Storefront.Shipping.OperationalShadow;
using Tooba.Order.Application.Storefront.Shipping.RateAuthority;

namespace Tooba.Order.Application.Storefront.Shipping.Queries.ProjectStorefrontShipping;

/// <summary>
/// درخواست تصویر ارسال فروشگاه. سه ورودی آخر <b>فقط مشاهده‌ای</b> هستند: هیچ‌کدام به قیمت، روش
/// انتخاب‌شده، promise یا پیش‌نویس سفارش راه نمی‌یابند و همه اختیاری‌اند تا مشتریان قدیمی که فقط
/// استان می‌فرستند بدون تغییر کار کنند.
/// </summary>
/// <param name="CartId">شناسهٔ سبد.</param>
/// <param name="GuestSecret">راز مهمان.</param>
/// <param name="ProvinceName">استان انتخاب‌شده (کد یا برچسب)؛ همان ورودی قدیمی مسیر.</param>
/// <param name="MethodCode">روش انتخابی.</param>
/// <param name="Language">زبان نمایش.</param>
/// <param name="CityName">شهر انتخاب‌شده (نام یا کد) در صورت وجود؛ دقت هویت مقصد را به شهر می‌رساند.</param>
/// <param name="Destination">شناسه‌های پایدار Geography در صورت وجود؛ پیش از مصرف اعتبارسنجی می‌شوند.</param>
public sealed record ProjectStorefrontShippingQuery(
    Guid CartId,
    string? GuestSecret,
    string? ProvinceName,
    string? MethodCode,
    string? Language,
    string? CityName = null,
    DestinationIdentifiers? Destination = null) : IRequest<Result<StorefrontShippingProjection>>;

public sealed class ProjectStorefrontShippingHandler(
    StorefrontShippingService shipping,
    StorefrontShippingShadowObserver shadow,
    StorefrontShippingOperationalShadowObserver operationalShadow)
    : IRequestHandler<ProjectStorefrontShippingQuery, Result<StorefrontShippingProjection>>
{
    public async Task<Result<StorefrontShippingProjection>> Handle(
        ProjectStorefrontShippingQuery request,
        CancellationToken cancellationToken)
    {
        // مسیر قدیمی دقیقاً همان پنج ورودی قبلی را می‌گیرد؛ شهر و شناسه‌های مقصد فقط برای دروازهٔ
        // نرخ مالک (پیش‌فرض خاموش) به سرویس می‌رسند و در حالت خاموش هیچ اثری ندارند.
        var result = await StorefrontOrderResult.ExecuteAsync(() => shipping.ProjectAsync(
            request.CartId, request.GuestSecret, request.ProvinceName, request.MethodCode, request.Language, cancellationToken,
            request.CityName, request.Destination));

        // مشاهدهٔ سایه روی همان projection واقعی: فقط-مشاهده. نتیجهٔ بازگشتی دست‌نخورده است و
        // هیچ مقدار مشاهده‌شده به روش انتخاب، قیمت مشتری، promise یا draft راه نمی‌یابد.
        if (result.IsSuccess && result.Value is { } projection)
        {
            await shadow.ObserveAsync(
                new StorefrontShippingShadowObservationInput(
                    request.ProvinceName,
                    projection.SubtotalExclusiveOfTax,
                    projection.Currency,
                    projection.Methods
                        .Select(m => new StorefrontShippingShadowCandidate(m.ServiceCode, m.PriceAmount, m.OptionCode))
                        .ToArray())
                {
                    CityName = request.CityName,
                    Destination = request.Destination,
                },
                cancellationToken);

            // سایهٔ عملیاتی درز <b>جدا</b> و پیش‌فرض‌خاموش است: ارزیابی کامل (بسته‌بندی، تفکیک پرداخت،
            // بازهٔ حامل، ظرفیت). نتیجهٔ آن هم دور ریخته می‌شود و هیچ مبلغ یا وعدهٔ مشتری را تغییر نمی‌دهد.
            //
            // اندازهٔ فیزیکی واقعی از این درخواست عمومی خوانده <b>نمی‌شود</b>: مسیر فروشگاه امروز هیچ
            // منبع اندازهٔ معتبری ندارد و گرفتن آن از بدنهٔ درخواست، یک مقدار دلخواه کاربر را به
            // ورودی تجاری تبدیل می‌کند. پس اندازه عمداً اعلام‌نشده می‌ماند و اگر سیاست شرط‌دار باشد،
            // نتیجه صریحاً «اندازه در دسترس نیست» است، نه یک تطابق ساختگی.
            await operationalShadow.ObserveAsync(
                new StorefrontShippingOperationalShadowInput(
                    request.ProvinceName,
                    projection.SubtotalExclusiveOfTax,
                    projection.Currency,
                    projection.Methods
                        .Select(m => new StorefrontShippingOperationalShadowCandidate(
                            m.ServiceCode,
                            m.OptionCode,
                            m.PriceAmount,
                            m.LeadDays))
                        .ToArray())
                {
                    CityName = request.CityName,
                    Destination = request.Destination,
                },
                cancellationToken);
        }

        return result;
    }
}
