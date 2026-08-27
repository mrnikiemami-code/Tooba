namespace Tooba.Notification.Infrastructure;

/// <summary>
/// تله‌متری سبک Notification بدون محتوای خصوصی پیام.
/// </summary>
public sealed class NotificationInstrumentation
{
    private long _created;
    private long _duplicateSuppressed;
    private long _readTransitions;

    /// <summary>اعلان جدید.</summary>
    public void RecordCreated(string sourceType, string recipientKind)
    {
        _ = sourceType;
        _ = recipientKind;
        Interlocked.Increment(ref _created);
    }

    /// <summary>تکرار رویداد سرکوب شد.</summary>
    public void RecordDuplicateSuppressed(string sourceType) =>
        Interlocked.Increment(ref _duplicateSuppressed);

    /// <summary>انتقال به خوانده‌شده.</summary>
    public void RecordReadTransition() => Interlocked.Increment(ref _readTransitions);

    /// <summary>شمارندهٔ ایجاد برای تست.</summary>
    public long CreatedCount => Interlocked.Read(ref _created);

    /// <summary>شمارندهٔ duplicate برای تست.</summary>
    public long DuplicateSuppressedCount => Interlocked.Read(ref _duplicateSuppressed);
}
