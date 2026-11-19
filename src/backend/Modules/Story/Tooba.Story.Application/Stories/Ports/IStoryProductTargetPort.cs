namespace Tooba.Story.Application.Stories.Ports;

/// <summary>مرجع مالکیت محصول/پیشنهاد برای هدف‌های CTA استوری محدود به فروشگاه.</summary>
public interface IStoryProductTargetPort
{
    /// <summary>
    /// تأیید می‌کند محصول هدف برقرار و متعلق به <paramref name="sellerPartyId"/> است.
    /// محصول غایب/بیگانه رد می‌شود؛ مالکیت جعل نمی‌شود.
    /// </summary>
    Task EnsureOwnedBySellerAsync(Guid productId, Guid sellerPartyId, CancellationToken cancellationToken);
}
