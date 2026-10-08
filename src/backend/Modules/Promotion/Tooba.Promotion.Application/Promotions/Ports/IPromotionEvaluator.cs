namespace Tooba.Promotion.Application.Promotions.Ports;

/// <summary>
/// ارزیابی قطعی پروموشن روی واقعیت‌های ورودی قرارداد.
/// </summary>
public interface IPromotionEvaluator
{
    /// <summary>
    /// تخفیف را روی مبلغ بدون مالیات خط حساب می‌کند. ارز نامطابق مبلغ ثابت را اعمال نمی‌کند.
    /// </summary>
    Task<PromotionEvaluationResult> EvaluateAsync(PromotionEvaluationRequest request, CancellationToken cancellationToken);
}
