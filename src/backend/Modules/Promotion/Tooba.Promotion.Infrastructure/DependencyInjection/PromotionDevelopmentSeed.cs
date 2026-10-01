using Microsoft.Extensions.DependencyInjection;
using Tooba.Promotion.Application.Merchandising;
using Tooba.Promotion.Contracts.Merchandising;
using Tooba.Promotion.Infrastructure.Development;

namespace Tooba.Promotion.Infrastructure.DependencyInjection;

/// <summary>
/// گام‌های پس از مهاجرت schema که مالکیت آن‌ها با Promotion است و میزبان نباید جزئیاتشان را بداند.
/// فعلاً: تضمین idempotent گونهٔ AMAZING و کمپین‌های Development (LOCK-SF-402).
/// </summary>
public static class PromotionDevelopmentSeed
{
    /// <summary>
    /// گونهٔ سیستمی AMAZING و کمپین‌های Development را idempotent تضمین می‌کند.
    /// خارج از Development بی‌اثر است.
    /// </summary>
    public static Task EnsureAsync(IServiceProvider provider, CancellationToken cancellationToken) =>
        MerchandisingCampaignDevelopmentSeed.EnsureAsync(provider, cancellationToken);

    /// <summary>
    /// گونهٔ سیستمی AMAZING را idempotent تضمین می‌کند.
    /// </summary>
    public static Task EnsureAmazingTypeAsync(IServiceProvider provider, CancellationToken cancellationToken) =>
        provider.GetRequiredService<IMerchandisingCampaignDirectory>()
            .EnsureAmazingTypeSeededAsync(cancellationToken);
}
