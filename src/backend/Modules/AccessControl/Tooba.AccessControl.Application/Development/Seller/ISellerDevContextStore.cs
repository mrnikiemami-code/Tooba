namespace Tooba.AccessControl.Application.Development.Seller;

/// <summary>
/// درز Application-owned برای قابلیت Development فروشنده: آماده‌سازی بازیگران demo و نگه‌داشتن نگاشت Actor↔Seller.
/// پیاده‌سازی در AccessControl.Infrastructure/Development/Seller است تا Endpoints به Infrastructure وابسته نشود.
/// </summary>
public interface ISellerDevContextStore
{
    /// <summary>آخرین snapshot ساخته‌شده یا <see langword="null"/> وقتی bootstrap هنوز اجرا نشده است.</summary>
    SellerDevContextSnapshot? Current { get; }

    /// <summary>
    /// کاربران demo، عضویت Party و tupleهای مجوز را آماده می‌کند و در صورت موفقیت snapshot را منتشر می‌کند.
    /// باید روی همان scope با CommerceContext انتساب‌شده صدا زده شود؛ scope تو در تو نمی‌سازد.
    /// </summary>
    Task EnsureAsync(CancellationToken cancellationToken);

    /// <summary>snapshot فروشندهٔ marketplace را جایگزین می‌کند (بدون seed فروشگاهی).</summary>
    void Publish(SellerDevActorPair actorA, SellerDevActorPair actorB);

    /// <summary>کارمند محدود را در صورت وجود snapshot اضافه می‌کند.</summary>
    void PublishScopedEmployee(SellerDevActorPair employee);
}
