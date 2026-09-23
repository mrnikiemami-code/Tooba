namespace Tooba.Cart.Application.Ports;

/// <summary>ساعت ماندگاری سبد را از Settings حل می‌کند؛ TTL رزرو نیست.</summary>
public interface ICartPersistenceHoursSource
{
    /// <summary>ساعت نگهداری سبد پس از آخرین جهش؛ سیاست مالکیت Cart.</summary>
    /// <param name="cancellationToken">لغو درخواست.</param>
    Task<int> ResolvePersistenceHoursAsync(CancellationToken cancellationToken);
}
