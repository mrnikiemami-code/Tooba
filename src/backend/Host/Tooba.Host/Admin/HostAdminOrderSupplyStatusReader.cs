using Tooba.Order.Application.Admin.OrdersGrid.Ports;

namespace Tooba.Host.Admin;

/// <summary>
/// درز باریک وضعیت تأمین برای گرید سفارش‌های Order.
/// ترکیب‌گر تأمین تا R4 در Host می‌ماند؛ Order فقط نام پایدار وضعیت را می‌بیند.
/// </summary>
internal sealed class HostAdminOrderSupplyStatusReader(OrderSupplyComposer supply) : IAdminOrderSupplyStatusReader
{
    public async Task<IReadOnlyDictionary<Guid, string>> GetStatusesAsync(
        IReadOnlyList<Guid> checkoutIds,
        CancellationToken cancellationToken)
    {
        var statuses = await supply.GetStatusesAsync(checkoutIds, cancellationToken);
        return statuses.ToDictionary(x => x.Key, x => x.Value.Status.ToString());
    }
}
