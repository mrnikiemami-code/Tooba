using MediatR;
using Tooba.BuildingBlocks.Results;
using Tooba.Payment.Application.Composition;
using Tooba.Payment.Application.Storefront.Models;

namespace Tooba.Payment.Application.Storefront.Queries;

/// <summary>
/// نقل‌قول کیف‌پول برای دو سطح مالکیت‌یافتهٔ متفاوت، با یک قرارداد transport واحد:
/// <list type="bullet">
/// <item><c>CartScoped = false</c> (پیش‌فرض): مسیر سفارش متعهد؛ <see cref="CheckoutId"/> اجباری است و
/// مبلغ از مالکیت سفارش (Order 025) می‌آید — رفتار پیشین دست‌نخورده.</item>
/// <item><c>CartScoped = true</c>: مسیر پیش‌از‌سفارش <c>/payment?cartId=...</c>؛ مبلغ فقط از ارزیابی
/// authoritative سبد (Order 011/031) می‌آید، <see cref="CheckoutId"/> باید غایب باشد و
/// کیف‌پول تنها برای خریدار احراز‌شده‌ای که سرور پوشش کامل مبلغ را اثبات کند قابل انتخاب است.</item>
/// </list>
/// در هیچ حالتی مبلغ/موجودی/اهلیت از مرورگر پذیرفته نمی‌شود.
/// </summary>
public sealed record GetStorefrontWalletQuoteQuery(
    Guid? CheckoutId,
    Guid CartId,
    string? GuestSecret,
    Guid? AuthenticatedUserId,
    bool CartScoped = false) : IRequest<Result<StorefrontWalletQuoteDto>>;

public sealed class GetStorefrontWalletQuoteHandler(StorefrontPaymentOrchestrator orchestrator)
    : IRequestHandler<GetStorefrontWalletQuoteQuery, Result<StorefrontWalletQuoteDto>>
{
    public Task<Result<StorefrontWalletQuoteDto>> Handle(
        GetStorefrontWalletQuoteQuery request, CancellationToken cancellationToken) =>
        request.CartScoped
            ? PaymentOperation.ExecuteAsync(() => orchestrator.GetCartWalletQuoteAsync(
                request.CartId,
                request.GuestSecret,
                request.AuthenticatedUserId,
                cancellationToken))
            : PaymentOperation.ExecuteAsync(() => orchestrator.GetWalletQuoteAsync(
                request.CheckoutId!.Value,
                request.CartId,
                request.GuestSecret,
                request.AuthenticatedUserId,
                cancellationToken));
}
