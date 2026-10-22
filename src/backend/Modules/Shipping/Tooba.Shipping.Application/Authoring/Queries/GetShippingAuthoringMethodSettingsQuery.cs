using MediatR;
using Tooba.BuildingBlocks.Results;
using Tooba.Shipping.Application.Authoring.Models;
using Tooba.Shipping.Application.Authoring.Ports;

namespace Tooba.Shipping.Application.Authoring.Queries;

/// <summary>
/// خواندن تنظیمات روش یک پیش‌نویس مالک: زمان پرداخت کرایه و حالت محاسبه، هر دو مستقل.
/// </summary>
/// <param name="Owner">مالک مجاز همین درخواست.</param>
/// <param name="DraftId">شناسهٔ پیش‌نویس از مسیر.</param>
public sealed record GetShippingAuthoringMethodSettingsQuery(
    ShippingAuthoringOwnerScope Owner,
    Guid DraftId)
    : IRequest<Result<ShippingAuthoringMethodSettingsItem>>;

/// <summary>
/// هندلر خواندن تنظیمات روش. تنظیمات فقط داخل محدودهٔ کامل مالک مجاز خوانده می‌شود؛ پیش‌نویس غایب و
/// پیش‌نویس مالک دیگر همان پاسخ ایمن «پیدا نشد» را می‌گیرند.
/// </summary>
public sealed class GetShippingAuthoringMethodSettingsHandler(IShippingAuthoringDraftDirectory directory)
    : IRequestHandler<GetShippingAuthoringMethodSettingsQuery, Result<ShippingAuthoringMethodSettingsItem>>
{
    /// <inheritdoc />
    public async Task<Result<ShippingAuthoringMethodSettingsItem>> Handle(
        GetShippingAuthoringMethodSettingsQuery request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(request.Owner);

        var draft = await directory.GetAsync(request.Owner, request.DraftId, cancellationToken);
        return draft.IsFailure
            ? Result.Failure<ShippingAuthoringMethodSettingsItem>(draft.Errors)
            : Result.Success(ShippingAuthoringMethodSettingsWorkflow.Map(draft.Value));
    }
}

/// <summary>
/// نگاشت پیش‌نویس به سند تنظیمات روش. «تنظیم‌نشده» هیچ مقدار پیش‌فرضی نمی‌گیرد و هیچ ادعای آمادگی
/// عملیاتی، اتصال حامل یا تأمین مالی ساخته نمی‌شود.
/// </summary>
internal static class ShippingAuthoringMethodSettingsWorkflow
{
    /// <summary>پیش‌نویس را به پاسخ تنظیمات روش نگاشت می‌کند.</summary>
    /// <param name="draft">پیش‌نویس مالک.</param>
    public static ShippingAuthoringMethodSettingsItem Map(Domain.Authoring.ShippingAuthoringDraft draft)
    {
        ArgumentNullException.ThrowIfNull(draft);

        var settings = draft.MethodSettings;
        return new ShippingAuthoringMethodSettingsItem(
            draft.DraftId,
            draft.Revision,
            settings is not null,
            settings?.FreightPaymentMode,
            settings?.CalculationMode);
    }
}
