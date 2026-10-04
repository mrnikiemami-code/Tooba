namespace Tooba.Fulfillment.Application.Shipping.Ports;

/// <summary>نوشتن کاتالوگ سرویس ارسال روی مالک Fulfillment.</summary>
public interface IShippingServiceDirectory
{
    /// <summary>سرویس جدید.</summary>
    Task<Guid> CreateAsync(ShippingServiceWriteModel model, CancellationToken cancellationToken);

    /// <summary>ویرایش سرویس، ترجمه‌ها و گزینه‌ها.</summary>
    Task<Guid> UpdateAsync(Guid serviceId, ShippingServiceWriteModel model, CancellationToken cancellationToken);

    /// <summary>غیرفعال‌سازی نرم.</summary>
    Task DeactivateAsync(Guid serviceId, CancellationToken cancellationToken);

    /// <summary>seed اولیهٔ idempotent وقتی جدول خالی است.</summary>
    Task EnsureSeedAsync(CancellationToken cancellationToken);
}
