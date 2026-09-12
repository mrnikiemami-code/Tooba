namespace Tooba.Cart.Application;

/// <summary>ساعت ماندگاری سبد را از Settings حل می‌کند؛ TTL رزرو نیست.</summary>
public interface ICartPersistenceHoursSource
{
    /// <summary>ساعت نگهداری سبد پس از آخرین جهش.</summary>
    int ResolvePersistenceHours();
}
