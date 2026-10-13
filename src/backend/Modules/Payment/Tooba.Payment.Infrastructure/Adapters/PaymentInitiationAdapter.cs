#pragma warning disable CS1591
using Tooba.Payment.Application.Storefront.Models;
using Tooba.Payment.Application.Storefront.Orchestration;
using Tooba.Payment.Contracts.Storefront;

namespace Tooba.Payment.Infrastructure.Adapters;

/// <summary>
/// درز Contracts-only برای شروع پرداخت از ماژول مالک Order بدون ارجاع به Payment.Application/Infrastructure.
/// تنها به مسیر شروع موجود <see cref="StorefrontPaymentOrchestrator"/> وصل می‌شود؛ صلاحیت درگاه،
/// actor policy، رفتار online/wallet/manual و idempotency هیچ‌کدام اینجا بازپیاده‌سازی نمی‌شوند.
/// خطاهای Payment دست‌نخورده عبور می‌کنند و به خطای عمومی تبدیل نمی‌شوند.
/// </summary>
/// <param name="orchestrator">
/// رزولوشن تعویق‌افتادهٔ ارکستریتور Payment. دقیقاً مثل <c>Func&lt;IPaymentAdminDirectory&gt;</c> در
/// <c>PaymentDirectory</c>، این Func نگاشت گراف ماژول را از ثبت Host جدا می‌کند و اعتبارسنجی DI ماژول
/// را مستقل نگه می‌دارد؛ هیچ قاعدهٔ پرداختی اینجا تکرار نمی‌شود.
/// </param>
public sealed class PaymentInitiationAdapter(Func<StorefrontPaymentOrchestrator> orchestrator) : IPaymentInitiationPort
{
    /// <inheritdoc />
    public async Task<StorefrontPaymentInitiationSnapshot> InitiateForCheckoutAsync(
        StorefrontPaymentInitiationRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var initiated = await orchestrator().InitiateAsync(
            request.CheckoutId,
            request.CartId,
            request.GuestSecret,
            request.IdempotencyKey,
            request.UseWallet,
            request.ProviderCode,
            request.AuthenticatedUserId,
            cancellationToken);

        return Map(initiated);
    }

    /// <summary>
    /// نگاشت یک‌به‌یک خروجی شروع پرداخت. هیچ فیلدی ساخته، حذف یا بازتعریف نمی‌شود.
    /// </summary>
    internal static StorefrontPaymentInitiationSnapshot Map(StorefrontPaymentInitiationDto dto)
    {
        ArgumentNullException.ThrowIfNull(dto);

        return new StorefrontPaymentInitiationSnapshot(
            dto.PaymentId,
            dto.AttemptId,
            dto.CheckoutId,
            dto.Status,
            dto.ProviderCode,
            dto.ProviderRequestReference,
            dto.RedirectUrl,
            dto.Amount,
            dto.Currency,
            dto.RequiresPspRedirect);
    }
}
