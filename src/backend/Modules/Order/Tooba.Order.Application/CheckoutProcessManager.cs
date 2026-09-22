using System.Transactions;
using Microsoft.Extensions.Logging;
using Tooba.BuildingBlocks;
using Tooba.Cart.Contracts;
using Tooba.Inventory.Contracts.Availability;
using Tooba.Inventory.Contracts.Checkout;
using Tooba.Inventory.Contracts.Errors;
using Tooba.Inventory.Contracts.Orders;
using Tooba.Inventory.Contracts.Seller;
using Tooba.Order.Domain;

namespace Tooba.Order.Application;

/// <summary>
/// هماهنگ‌کنندهٔ در-فرآیند و همزمان checkout (Process Manager).
/// بدون DbContext، بدون پیام توزیع‌شده، بدون جبران ناهمگام.
/// </summary>
public sealed class CheckoutProcessManager : ICheckoutProcessManager
{
    private readonly ICheckoutSubmitHost _host;
    private readonly ICheckoutInventoryReservationPort _inventory;
    private readonly ICartConversionPort _cartConversion;
    private readonly ICheckoutProcessTracker? _processes;
    private readonly IClock _clock;
    private readonly IIdGenerator _ids;
    private readonly ILogger<CheckoutProcessManager> _logger;

    /// <summary>Process Manager را می‌سازد.</summary>
    public CheckoutProcessManager(
        ICheckoutSubmitHost host,
        ICheckoutInventoryReservationPort inventory,
        ICartConversionPort cartConversion,
        IClock clock,
        IIdGenerator ids,
        ILogger<CheckoutProcessManager> logger,
        ICheckoutProcessTracker? processes = null)
    {
        _host = host;
        _inventory = inventory;
        _cartConversion = cartConversion;
        _clock = clock;
        _ids = ids;
        _logger = logger;
        _processes = processes;
    }

    /// <inheritdoc />
    public async Task<CheckoutSnapshot> SubmitAsync(SubmitCheckoutCommand command, CancellationToken cancellationToken)
    {
        await _host.EnsureCanMutateAsync(cancellationToken);
        var existing = await _host.FindExistingCheckoutAsync(command, cancellationToken);
        if (existing is not null)
        {
            _host.EnsureCheckoutAccess(existing, command);
            await _host.ReconcileCartConversionAsync(existing, command, cancellationToken);
            return existing;
        }

        var cart = await _host.LoadActiveCartAsync(command, cancellationToken);
        var now = _clock.UtcNow;
        var checkoutId = _ids.NewId();
        CheckoutProcess? process = null;
        if (_processes is not null)
        {
            process = await _processes.BeginAsync(command.IdempotencyKey, cart.CartId, now, cancellationToken);
            if (process.IsSubmitSucceeded && process.CheckoutId is { } existingCheckoutId)
            {
                var prior = await _host.FindCheckoutByIdAsync(existingCheckoutId, cancellationToken);
                if (prior is not null)
                {
                    _host.EnsureCheckoutAccess(prior, command);
                    return prior;
                }
            }

            _processes.MarkValidating(process, now);
        }

        var sellerOrders = await _host.QuoteSellerOrdersAsync(
            cart,
            command,
            checkoutId,
            now,
            new Dictionary<Guid, Guid>(),
            requireReservation: false,
            cancellationToken);

        IReadOnlyDictionary<Guid, Guid>? reservations = null;
        var abuseSettings = await _host.LoadAbuseSettingsAsync(cancellationToken);
        try
        {
            using var scope = new TransactionScope(
                TransactionScopeOption.Required,
                new TransactionOptions { IsolationLevel = IsolationLevel.ReadCommitted },
                TransactionScopeAsyncFlowOption.Enabled);

            await _host.EnsureCanStartInitialReservationAsync(command.PlacedByUserId, abuseSettings, cancellationToken);
            if (process is not null)
            {
                _processes!.MarkInventoryReserving(process, now);
            }

            var expiresAt = await _host.ResolveInitialReservationExpiresAtAsync(cart, now, cancellationToken);
            var correlationId = process?.CorrelationId ?? $"checkout-submit:{command.IdempotencyKey.Trim()}";
            var reserveRequest = new CheckoutInventoryReservationRequest(
                cart.CartId,
                process?.ProcessId,
                correlationId,
                now,
                expiresAt,
                cart.Lines.Select(line => new CheckoutInventoryLineRequest(
                    line.LineId,
                    line.OfferId,
                    line.Quantity,
                    line.ReservationId)).ToArray());
            var reserved = await _inventory.ReserveForCheckoutAsync(reserveRequest, cancellationToken);
            reservations = reserved.ByCartLineId;
            await _host.OnAfterReserveAsync(cancellationToken);

            var group = await _host.PersistCheckoutAsync(
                checkoutId,
                command,
                cart,
                sellerOrders,
                reservations,
                now,
                abuseSettings,
                process,
                cancellationToken);

            await _host.OnAfterOrderWriteAsync(cancellationToken);

            if (process is not null)
            {
                _processes!.MarkCartCommitting(process, now);
                await _host.SaveProcessMilestonesAsync(cancellationToken);
            }

            var intent = command.Mode == OrderMode.RequestToReserve
                ? CartConversionIntent.RequestToReserve
                : CartConversionIntent.OnlinePurchase;
            try
            {
                await _cartConversion.ConvertForCheckoutAsync(
                    new CartConversionRequest(
                        group.CartId,
                        command.CartAccess,
                        cart.Version,
                        intent,
                        process?.ProcessId,
                        correlationId),
                    cancellationToken);
            }
            catch (InvalidOperationException)
            {
                var raced = await _host.TryResolveConvertedCartWinnerAsync(command, cancellationToken);
                if (raced is null)
                {
                    throw;
                }

                if (process is not null)
                {
                    _processes!.MarkPaymentPending(process, now);
                    await _host.SaveProcessMilestonesAsync(cancellationToken);
                }

                scope.Complete();
                return raced;
            }

            await _host.OnAfterCartConvertedWriteAsync(cancellationToken);
            if (process is not null)
            {
                _processes!.MarkPaymentPending(process, now);
                await _host.SaveProcessMilestonesAsync(cancellationToken);
            }

            scope.Complete();
            return group;
        }
        catch (CheckoutConflictException)
        {
            var winner = await _host.FindConflictWinnerAsync(command, cancellationToken);
            if (winner is null)
            {
                throw;
            }

            _host.EnsureCheckoutAccess(winner, command);
            await _host.ReconcileCartConversionAsync(winner, command, cancellationToken);
            return winner;
        }
        // Inventory still exposes this W1-W5 conflict as a legacy exception code.
        // Keep the frozen winner-reconciliation behavior until R1 adds a typed Contracts error.
        catch (InvalidOperationException ex) when (ex.Message == "inventory.reservation.conflict")
        {
            var winner = await _host.FindConflictWinnerAsync(command, cancellationToken);
            if (winner is null)
            {
                throw;
            }

            _host.EnsureCheckoutAccess(winner, command);
            await _host.ReconcileCartConversionAsync(winner, command, cancellationToken);
            return winner;
        }
        catch
        {
            if (reservations is { Count: > 0 })
            {
                try
                {
                    await _inventory.ReleaseAsync(reservations.Values, cancellationToken);
                }
                catch (InvalidOperationException ex)
                {
                    _logger.LogWarning(
                        ex,
                        "Failed to release checkout reservations after submit failure. CheckoutId={CheckoutId}",
                        checkoutId);
                }
            }

            throw;
        }
    }
}

/// <summary>Signals a stable checkout uniqueness/concurrency conflict without message classification.</summary>
public sealed class CheckoutConflictException : Exception
{
    /// <summary>Creates a checkout conflict.</summary>
    public CheckoutConflictException(Exception? innerException = null)
        : base("order.checkout.conflict", innerException)
    {
    }
}

/// <summary>درز Application برای Process Manager checkout.</summary>
public interface ICheckoutProcessManager
{
    /// <summary>ارسال checkout با مالکیت orchestration در Process Manager.</summary>
    Task<CheckoutSnapshot> SubmitAsync(SubmitCheckoutCommand command, CancellationToken cancellationToken);
}

/// <summary>عملیات Order/Cart سمت شرکت‌کننده برای Process Manager (بدون منطق موجودی).</summary>
public interface ICheckoutSubmitHost
{
    /// <summary>مجوز mutate.</summary>
    Task EnsureCanMutateAsync(CancellationToken cancellationToken);

    /// <summary>checkout موجود با کلید/سبد.</summary>
    Task<CheckoutSnapshot?> FindExistingCheckoutAsync(SubmitCheckoutCommand command, CancellationToken cancellationToken);

    /// <summary>checkout با شناسه.</summary>
    Task<CheckoutSnapshot?> FindCheckoutByIdAsync(Guid checkoutId, CancellationToken cancellationToken);

    /// <summary>دسترسی.</summary>
    void EnsureCheckoutAccess(CheckoutSnapshot checkout, SubmitCheckoutCommand command);

    /// <summary>هم‌تراز کردن تبدیل سبد.</summary>
    Task ReconcileCartConversionAsync(CheckoutSnapshot checkout, SubmitCheckoutCommand command, CancellationToken cancellationToken);

    /// <summary>سبد فعال و معتبر.</summary>
    Task<CartSnapshot> LoadActiveCartAsync(SubmitCheckoutCommand command, CancellationToken cancellationToken);

    /// <summary>نقل‌قول سفارش‌های فروشنده.</summary>
    Task<IReadOnlyList<SellerOrder>> QuoteSellerOrdersAsync(
        CartSnapshot cart,
        SubmitCheckoutCommand command,
        Guid checkoutId,
        DateTimeOffset now,
        IReadOnlyDictionary<Guid, Guid> reservationsByCartLineId,
        bool requireReservation,
        CancellationToken cancellationToken);

    /// <summary>تنظیمات abuse.</summary>
    Task<CheckoutAbuseSettingsSnapshot> LoadAbuseSettingsAsync(CancellationToken cancellationToken);

    /// <summary>شروع رزرو اولیه.</summary>
    Task EnsureCanStartInitialReservationAsync(
        Guid placedByUserId,
        CheckoutAbuseSettingsSnapshot abuseSettings,
        CancellationToken cancellationToken);

    /// <summary>مهلت رزرو اولیه.</summary>
    Task<DateTimeOffset> ResolveInitialReservationExpiresAtAsync(
        CartSnapshot cart,
        DateTimeOffset now,
        CancellationToken cancellationToken);

    /// <summary>پس از رزرو.</summary>
    Task OnAfterReserveAsync(CancellationToken cancellationToken);

    /// <summary>پایدارسازی checkout + چرخه + abuse داخل TX جاری.</summary>
    Task<CheckoutSnapshot> PersistCheckoutAsync(
        Guid checkoutId,
        SubmitCheckoutCommand command,
        CartSnapshot cart,
        IReadOnlyList<SellerOrder> sellerOrders,
        IReadOnlyDictionary<Guid, Guid> reservations,
        DateTimeOffset now,
        CheckoutAbuseSettingsSnapshot abuseSettings,
        CheckoutProcess? process,
        CancellationToken cancellationToken);

    /// <summary>پس از نوشتن سفارش.</summary>
    Task OnAfterOrderWriteAsync(CancellationToken cancellationToken);

    /// <summary>ذخیرهٔ milestone فرآیند.</summary>
    Task SaveProcessMilestonesAsync(CancellationToken cancellationToken);


    /// <summary>پس از تبدیل سبد.</summary>
    Task OnAfterCartConvertedWriteAsync(CancellationToken cancellationToken);

    /// <summary>برندهٔ تبدیل همزمان.</summary>
    Task<CheckoutSnapshot?> TryResolveConvertedCartWinnerAsync(
        SubmitCheckoutCommand command,
        CancellationToken cancellationToken);

    /// <summary>برندهٔ تعارض یکتایی.</summary>
    Task<CheckoutSnapshot?> FindConflictWinnerAsync(SubmitCheckoutCommand command, CancellationToken cancellationToken);
}
