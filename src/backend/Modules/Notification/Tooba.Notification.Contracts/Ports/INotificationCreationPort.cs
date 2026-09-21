using Tooba.Notification.Contracts.Commands;

namespace Tooba.Notification.Contracts.Ports;

/// <summary>
/// پورت ایجاد اعلان پایدار برای ماژول‌های خارجی (مثلاً Wallet).
/// فقط قرارداد ایجاد؛ بدون Domain/Application.
/// </summary>
public interface INotificationCreationPort
{
    /// <summary>اگر SourceEventId تکراری باشد ایجاد نمی‌کند. true = ایجاد شد.</summary>
    Task<bool> CreateIfAbsentAsync(CreateNotificationCommand command, CancellationToken cancellationToken);
}
