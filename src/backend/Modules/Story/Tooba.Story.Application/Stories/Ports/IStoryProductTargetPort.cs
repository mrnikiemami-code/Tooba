namespace Tooba.Story.Application.Stories.Ports;

/// <summary>مرجع اعتبار و مالکیت محصول برای هدف‌های CTA استوری.</summary>
public interface IStoryProductTargetPort
{
    /// <summary>
    /// تأیید می‌کند محصول هدف برقرار و متعلق به <paramref name="sellerPartyId"/> است.
    /// محصول غایب/بیگانه رد می‌شود؛ مالکیت جعل نمی‌شود.
    /// </summary>
    Task EnsureOwnedBySellerAsync(Guid productId, Guid sellerPartyId, CancellationToken cancellationToken);

    /// <summary>
    /// برای استوری ویترین اصلی تأیید می‌کند محصول در Catalog به‌صورت مثبت برقرار است.
    /// بدون الزام مالکیت فروشگاه؛ محصول غایب رد می‌شود.
    /// </summary>
    Task EnsurePublishedExistsAsync(Guid productId, CancellationToken cancellationToken);
}
