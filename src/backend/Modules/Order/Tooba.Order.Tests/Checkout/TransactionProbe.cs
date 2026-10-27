using System.Data.Common;
using System.Diagnostics;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace Tooba.Order.Tests.Checkout;

/// <summary>
/// TB-232-R2 — تنها seamهای آزمون برای اثبات طول‌عمر تراکنش و اولویت خطای اصلی.
/// </summary>
/// <remarks>
/// <para>
/// این کلاس هیچ hook تولیدی نیست: فقط روی <see cref="DbContextOptionsBuilder"/> همین آزمون نصب می‌شود و از
/// callbackهای واقعی EF (<see cref="ISaveChangesInterceptor"/>، <see cref="DbCommandInterceptor"/> و
/// <see cref="IDbTransactionInterceptor"/>) به‌همراه DiagnosticSource واقعی EF استفاده می‌کند. «Dispose واقعی»
/// از رویداد <c>TransactionDisposed</c> خود EF خوانده می‌شود، چون EF هیچ callback بین‌سپاری برای Dispose ندارد و
/// پاک شدن <c>Database.CurrentTransaction</c> اثبات Dispose نیست.
/// </para>
/// </remarks>
internal sealed class TransactionProbe :
    DbCommandInterceptor,
    IObserver<DiagnosticListener>,
    IObserver<KeyValuePair<string, object?>>,
    ISaveChangesInterceptor,
    IDbTransactionInterceptor,
    IDisposable
{
    private const string EfListener = "Microsoft.EntityFrameworkCore";
    private const string TransactionEventPrefix = "Microsoft.EntityFrameworkCore.Database.Transaction.";

    private readonly IDisposable _allListeners;
    private readonly List<string> _transactionEvents = [];
    private IDisposable? _listenerSubscription;

    private int _disposed;
    private int _committed;
    private int _rolledBack;
    private int _rollbackAttempts;
    private int _rollbackFaults;
    private int _saveCalls;
    private int _recoveryReads;
    private int _recoveryReadFaults;
    private int _primarySeen;
    private bool _failRecoveryRead;

    public TransactionProbe() => _allListeners = DiagnosticListener.AllListeners.Subscribe(this);

    /// <summary>هر رویداد <c>TransactionDisposed</c> خود EF برای تراکنش‌های همین context.</summary>
    public int Disposed => Volatile.Read(ref _disposed);

    /// <summary>تراکنش‌های commit‌شده (برای تفکیک «تمام‌شده» از «Dispose‌شده»).</summary>
    public int Committed => Volatile.Read(ref _committed);

    /// <summary>تراکنش‌های rollback‌شده.</summary>
    public int RolledBack => Volatile.Read(ref _rolledBack);

    /// <summary>هر تلاش Rollback (موفق یا ناموفق).</summary>
    public int RollbackAttempts => Volatile.Read(ref _rollbackAttempts);

    /// <summary>هر rollback که با خطای اسکریپت‌شده شکست خورد.</summary>
    public int RollbackFaults => Volatile.Read(ref _rollbackFaults);

    /// <summary>بازخوانی‌های جدول ریشه (شامل بازخوانی بازیابی).</summary>
    public int RecoveryReads => Volatile.Read(ref _recoveryReads);

    /// <summary>بازخوانی بازیابی که با خطای اسکریپت‌شده شکست خورد.</summary>
    public int RecoveryReadFaults => Volatile.Read(ref _recoveryReadFaults);

    /// <summary>نام رویدادهای تراکنش دیده‌شده (تشخیصی).</summary>
    public IReadOnlyList<string> TransactionEvents => _transactionEvents;

    /// <summary>خطای «اصلی» که EF از SaveChanges گزارش کرد (همان نمونه‌ای که باید منتشر شود).</summary>
    public Exception? PrimaryFailure { get; private set; }

    /// <summary>rollback را با خطای اسکریپت‌شده می‌شکند (شکست provider، نه «تراکنش بسته شده»).</summary>
    public bool FailRollback { get; set; }

    /// <summary>بازخوانی بازیابی را — فقط پس از خطای اصلی — می‌شکند.</summary>
    public bool FailRecoveryRead { get; set; }

    /// <summary>شمارهٔ SaveChanges (در همین پنجره) که باید توکن فراخوان را لغو کند.</summary>
    public int? CancelOnSaveNumber { get; set; }

    /// <summary>منبع لغوِ فراخوان که در مرز نوشتن لغو می‌شود.</summary>
    public CancellationTokenSource? Cancellation { get; set; }

    public void ResetCounters()
    {
        Volatile.Write(ref _disposed, 0);
        Volatile.Write(ref _committed, 0);
        Volatile.Write(ref _rolledBack, 0);
        Volatile.Write(ref _rollbackAttempts, 0);
        Volatile.Write(ref _rollbackFaults, 0);
        Volatile.Write(ref _saveCalls, 0);
        Volatile.Write(ref _recoveryReads, 0);
        Volatile.Write(ref _recoveryReadFaults, 0);
        Volatile.Write(ref _primarySeen, 0);
        _failRecoveryRead = false;
        PrimaryFailure = null;
        lock (_transactionEvents)
        {
            _transactionEvents.Clear();
        }
    }

    public void Dispose()
    {
        _listenerSubscription?.Dispose();
        _allListeners.Dispose();
    }

    // ---------------------------------------------------------------- DiagnosticSource

    void IObserver<DiagnosticListener>.OnNext(DiagnosticListener value)
    {
        if (string.Equals(value.Name, EfListener, StringComparison.Ordinal))
        {
            _listenerSubscription = value.Subscribe(this);
        }
    }

    void IObserver<KeyValuePair<string, object?>>.OnNext(KeyValuePair<string, object?> value)
    {
        if (!value.Key.StartsWith(TransactionEventPrefix, StringComparison.Ordinal))
        {
            return;
        }

        var name = value.Key[TransactionEventPrefix.Length..];
        lock (_transactionEvents)
        {
            _transactionEvents.Add(name);
        }

        switch (name)
        {
            case "TransactionDisposed":
                Interlocked.Increment(ref _disposed);
                break;
            case "TransactionCommitted":
                Interlocked.Increment(ref _committed);
                break;
            case "TransactionRolledBack":
                Interlocked.Increment(ref _rolledBack);
                break;
        }
    }

    void IObserver<DiagnosticListener>.OnCompleted() { }

    void IObserver<DiagnosticListener>.OnError(Exception error) { }

    void IObserver<KeyValuePair<string, object?>>.OnCompleted() { }

    void IObserver<KeyValuePair<string, object?>>.OnError(Exception error) { }

    // ---------------------------------------------------------------- SaveChanges

    public ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        var call = Interlocked.Increment(ref _saveCalls);
        if (CancelOnSaveNumber == call)
        {
            // لغو در مرز واقعیِ نوشتن: توکن خودِ فراخوان لغو می‌شود و همان توکن بالا می‌آید.
            CancelledAtSaveBoundary = true;
            Cancellation?.Cancel();
            cancellationToken.ThrowIfCancellationRequested();
        }

        return ValueTask.FromResult(result);
    }

    public Task SaveChangesFailedAsync(
        DbContextErrorEventData eventData,
        CancellationToken cancellationToken = default)
    {
        // همان نمونه‌ای که EF دوباره پرتاب می‌کند.
        PrimaryFailure = eventData.Exception;
        Volatile.Write(ref _primarySeen, 1);
        if (FailRecoveryRead)
        {
            _failRecoveryRead = true;
        }

        return Task.CompletedTask;
    }

    public Task SaveChangesCanceledAsync(
        DbContextEventData eventData,
        CancellationToken cancellationToken = default)
    {
        // EF لغو را از مسیر «Canceled» گزارش می‌کند، نه «Failed».
        CanceledToken = cancellationToken;
        if (FailRecoveryRead)
        {
            _failRecoveryRead = true;
        }

        return Task.CompletedTask;
    }

    /// <summary>توکنی که EF در مسیر لغوِ SaveChanges گزارش کرد.</summary>
    public CancellationToken? CanceledToken { get; private set; }

    /// <summary>آیا لغو دقیقاً در مرز نوشتن (نه پیش از فراخوان) رخ داد.</summary>
    public bool CancelledAtSaveBoundary { get; private set; }

    // ---------------------------------------------------------------- transaction

    public ValueTask<InterceptionResult> TransactionRollingBackAsync(
        DbTransaction transaction,
        TransactionEventData eventData,
        InterceptionResult result,
        CancellationToken cancellationToken = default)
    {
        Interlocked.Increment(ref _rollbackAttempts);
        if (FailRollback)
        {
            Interlocked.Increment(ref _rollbackFaults);
            // شکست واقعی rollback از دید آداپتور؛ نباید خطای اصلی را بپوشاند.
            throw new InvalidOperationException("scripted rollback failure");
        }

        return ValueTask.FromResult(result);
    }

    // ---------------------------------------------------------------- recovery read

    public override ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(
        DbCommand command,
        CommandEventData eventData,
        InterceptionResult<DbDataReader> result,
        CancellationToken cancellationToken = default)
    {
        if (command.CommandText.Contains("cart_shipping_drafts", StringComparison.Ordinal)
            && command.CommandText.Contains("SELECT", StringComparison.OrdinalIgnoreCase))
        {
            // فقط خواندن‌های پس از خطای اصلی، یعنی نامزدهای بازخوانی بازیابی، شمرده می‌شوند.
            if (Volatile.Read(ref _primarySeen) == 0)
            {
                return ValueTask.FromResult(result);
            }

            Interlocked.Increment(ref _recoveryReads);
            if (_failRecoveryRead)
            {
                Interlocked.Increment(ref _recoveryReadFaults);
                throw new InvalidOperationException("scripted recovery read failure");
            }
        }

        return ValueTask.FromResult(result);
    }
}
