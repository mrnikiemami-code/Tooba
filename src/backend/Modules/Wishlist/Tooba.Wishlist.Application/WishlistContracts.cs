namespace Tooba.Wishlist.Application;

/// <summary>نمای خصوصی یک ردیف علاقه‌مندی متعلق به کاربر جاری.</summary>
public sealed record WishlistEntry(Guid WishlistItemId, Guid ProductId, DateTimeOffset CreatedAt);

/// <summary>نتیجهٔ افزودن idempotent که مشخص می‌کند ردیف تازه ساخته شده است یا خیر.</summary>
public sealed record WishlistAddResult(Guid WishlistItemId, bool Created);

/// <summary>قرارداد کاربردی Wishlist؛ تمام عملیات با شناسهٔ Actor تأمین‌شده از Host محدود می‌شوند.</summary>
public interface IWishlistDirectory
{
    /// <summary>محصول Published را idempotent برای Actor می‌افزاید.</summary>
    Task<WishlistAddResult> AddAsync(Guid actorUserId, Guid productId, CancellationToken cancellationToken);
    /// <summary>مرجع محصول را برای Actor حذف می‌کند و نبودن آن خطا نیست.</summary>
    Task RemoveAsync(Guid actorUserId, Guid productId, CancellationToken cancellationToken);
    /// <summary>فهرست خصوصی Actor را به ترتیب جدیدترین برمی‌گرداند.</summary>
    Task<IReadOnlyList<WishlistEntry>> ListAsync(Guid actorUserId, CancellationToken cancellationToken);
    /// <summary>عضویت مجموعهٔ محصولات را در یک خواندن گروهی برمی‌گرداند.</summary>
    Task<IReadOnlySet<Guid>> GetMembershipAsync(Guid actorUserId, IReadOnlyCollection<Guid> productIds, CancellationToken cancellationToken);
    /// <summary>تعداد ردیف‌های خصوصی Actor را برمی‌گرداند.</summary>
    Task<long> CountAsync(Guid actorUserId, CancellationToken cancellationToken);
}
