using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using Tooba.BuildingBlocks;
using Tooba.Cart.Contracts;
using Tooba.Inventory.Contracts.Availability;
using Tooba.Inventory.Contracts.Checkout;
using Tooba.Inventory.Contracts.Errors;
using Tooba.Inventory.Contracts.Orders;
using Tooba.Inventory.Contracts.Seller;
using Tooba.Order.Application;
using Tooba.Order.Domain;

namespace Tooba.Order.Infrastructure;

public sealed partial class CheckoutDirectory : ICheckoutSubmitHost
{
    /// <inheritdoc />
    Task ICheckoutSubmitHost.EnsureCanMutateAsync(CancellationToken cancellationToken)
        => _guard.EnsureCanMutateAsync(cancellationToken);

    /// <inheritdoc />
    async Task<CheckoutSnapshot?> ICheckoutSubmitHost.FindExistingCheckoutAsync(
        SubmitCheckoutCommand command,
        CancellationToken cancellationToken)
    {
        var group = await FindCheckoutAsync(
            x => x.IdempotencyKey == command.IdempotencyKey.Trim() || x.CartId == command.CartId,
            cancellationToken);
        return group is null ? null : ToSnapshot(group);
    }

    /// <inheritdoc />
    async Task<CheckoutSnapshot?> ICheckoutSubmitHost.FindCheckoutByIdAsync(
        Guid checkoutId,
        CancellationToken cancellationToken)
    {
        var group = await FindCheckoutAsync(x => x.CheckoutId == checkoutId, cancellationToken);
        return group is null ? null : ToSnapshot(group);
    }

    /// <inheritdoc />
    void ICheckoutSubmitHost.EnsureCheckoutAccess(CheckoutSnapshot checkout, SubmitCheckoutCommand command)
    {
        var group = _db.Checkouts.Local.FirstOrDefault(x => x.CheckoutId == checkout.CheckoutId)
            ?? _db.Checkouts.Single(x => x.CheckoutId == checkout.CheckoutId);
        EnsureAccess(group, new OrderAccess(command.BuyerPartyId, command.PlacedByUserId));
    }

    /// <inheritdoc />
    async Task ICheckoutSubmitHost.ReconcileCartConversionAsync(
        CheckoutSnapshot checkout,
        SubmitCheckoutCommand command,
        CancellationToken cancellationToken)
    {
        var group = await FindCheckoutAsync(x => x.CheckoutId == checkout.CheckoutId, cancellationToken)
            ?? throw new InvalidOperationException("checkout پیدا نشد.");
        await ReconcileCartConversionAsync(group, command, cancellationToken);
    }

    /// <inheritdoc />
    async Task<CartSnapshot> ICheckoutSubmitHost.LoadActiveCartAsync(
        SubmitCheckoutCommand command,
        CancellationToken cancellationToken)
    {
        var cart = await _carts.GetCartAsync(command.CartId, command.CartAccess, cancellationToken)
            ?? throw new InvalidOperationException("سبد برای checkout پیدا نشد؛ CartId Bearer نیست.");
        if (cart.Status != CartStatus.Active)
        {
            throw new InvalidOperationException("فقط سبد Active به سفارش تبدیل می‌شود.");
        }

        if (cart.Version != command.ExpectedCartVersion)
        {
            throw new InvalidOperationException("نسخهٔ سبد کهنه است؛ checkout همزمان رد شد.");
        }

        if (cart.Lines.Count == 0)
        {
            throw new InvalidOperationException("سبد خالی به سفارش تبدیل نمی‌شود.");
        }

        return cart;
    }

    /// <inheritdoc />
    async Task<IReadOnlyList<SellerOrder>> ICheckoutSubmitHost.QuoteSellerOrdersAsync(
        CartSnapshot cart,
        SubmitCheckoutCommand command,
        Guid checkoutId,
        DateTimeOffset now,
        IReadOnlyDictionary<Guid, Guid> reservationsByCartLineId,
        bool requireReservation,
        CancellationToken cancellationToken)
        => await QuoteSellerOrdersAsync(
            cart,
            command,
            checkoutId,
            now,
            reservationsByCartLineId,
            requireReservation,
            cancellationToken);

    /// <inheritdoc />
    Task<CheckoutAbuseSettingsSnapshot> ICheckoutSubmitHost.LoadAbuseSettingsAsync(CancellationToken cancellationToken)
        => _abuseGate.LoadSettingsAsync(cancellationToken);

    /// <inheritdoc />
    Task ICheckoutSubmitHost.EnsureCanStartInitialReservationAsync(
        Guid placedByUserId,
        CheckoutAbuseSettingsSnapshot abuseSettings,
        CancellationToken cancellationToken)
        => _abuseGate.EnsureCanStartInitialReservationAsync(placedByUserId, abuseSettings, cancellationToken);

    /// <inheritdoc />
    Task<DateTimeOffset> ICheckoutSubmitHost.ResolveInitialReservationExpiresAtAsync(
        CartSnapshot cart,
        DateTimeOffset now,
        CancellationToken cancellationToken)
        => ResolveInitialCycleExpiresAtAsync(cart, now, cancellationToken);

    /// <inheritdoc />
    Task ICheckoutSubmitHost.OnAfterReserveAsync(CancellationToken cancellationToken)
        => _commitBarrier.OnAfterReserveAsync(cancellationToken);

    /// <inheritdoc />
    async Task<CheckoutSnapshot> ICheckoutSubmitHost.PersistCheckoutAsync(
        Guid checkoutId,
        SubmitCheckoutCommand command,
        CartSnapshot cart,
        IReadOnlyList<SellerOrder> sellerOrders,
        IReadOnlyDictionary<Guid, Guid> reservations,
        DateTimeOffset now,
        CheckoutAbuseSettingsSnapshot abuseSettings,
        CheckoutProcess? process,
        CancellationToken cancellationToken)
    {
        var group = CheckoutGroup.Submit(
            checkoutId,
            command.IdempotencyKey,
            cart.CartId,
            command.Mode,
            command.BuyerPartyId,
            command.PlacedByUserId,
            cart.Market,
            cart.Currency,
            cart.Channel,
            sellerOrders,
            now,
            command.RecipientName,
            command.ContactMobile,
            command.ProvinceName,
            command.CityName,
            command.PostalAddress,
            command.PostalCode,
            command.ShippingMethodCode,
            command.ShippingMethodLabel,
            command.ShippingAmount,
            command.MinimumDeliveryDate,
            command.RequestedDeliveryDate,
            command.RequestedDeliveryTimeWindow,
            command.CustomerNote,
            command.RecipientFirstName,
            command.RecipientLastName);
        BindReservationsToOrders(group, cart, reservations);
        if (process is not null && _processes is not null)
        {
            _processes.MarkOrderPersisting(process, group.CheckoutId, now);
        }

        _db.Checkouts.Add(group);
        await PrepareInitialCycleAsync(group, command.Mode, reservations.Values, now, cancellationToken);
        var primaryOrderId = group.SellerOrders.Select(x => x.SellerOrderId).First();
        _abuseGate.PrepareInitialCommit(
            command.PlacedByUserId,
            group.CheckoutId,
            primaryOrderId,
            abuseSettings,
            now);
        try
        {
            await _db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            _db.ChangeTracker.Clear();
            throw new InvalidOperationException("checkout.conflict");
        }

        return ToSnapshot(group);
    }

    /// <inheritdoc />
    Task ICheckoutSubmitHost.OnAfterOrderWriteAsync(CancellationToken cancellationToken)
        => _commitBarrier.OnAfterOrderWriteAsync(cancellationToken);

    /// <inheritdoc />
    Task ICheckoutSubmitHost.SaveProcessMilestonesAsync(CancellationToken cancellationToken)
        => _db.SaveChangesAsync(cancellationToken);


    /// <inheritdoc />
    Task ICheckoutSubmitHost.OnAfterCartConvertedWriteAsync(CancellationToken cancellationToken)
        => _commitBarrier.OnAfterCartConvertedWriteAsync(cancellationToken);

    /// <inheritdoc />
    async Task<CheckoutSnapshot?> ICheckoutSubmitHost.TryResolveConvertedCartWinnerAsync(
        SubmitCheckoutCommand command,
        CancellationToken cancellationToken)
    {
        var latestCart = await _carts.GetCartAsync(command.CartId, command.CartAccess, cancellationToken);
        if (latestCart?.Status != CartStatus.Converted)
        {
            return null;
        }

        _db.ChangeTracker.Clear();
        var raced = await _db.Checkouts
            .AsNoTracking()
            .Include(x => x.SellerOrders)
            .ThenInclude(x => x.Lines)
            .Where(x => x.CartId == command.CartId)
            .OrderBy(x => x.SubmittedAt)
            .FirstOrDefaultAsync(cancellationToken);
        return raced is null ? null : ToSnapshot(raced);
    }

    /// <inheritdoc />
    async Task<CheckoutSnapshot?> ICheckoutSubmitHost.FindConflictWinnerAsync(
        SubmitCheckoutCommand command,
        CancellationToken cancellationToken)
    {
        _db.ChangeTracker.Clear();
        await _db.Database.CloseConnectionAsync();
        CheckoutGroup? winner = null;
        for (var attempt = 0; attempt < 10 && winner is null; attempt++)
        {
            if (attempt > 0)
            {
                await Task.Delay(20 * attempt, cancellationToken);
            }

            winner = await _db.Checkouts
                .AsNoTracking()
                .Include(x => x.SellerOrders)
                .ThenInclude(x => x.Lines)
                .Where(x => x.CartId == command.CartId)
                .OrderBy(x => x.SubmittedAt)
                .FirstOrDefaultAsync(cancellationToken);
        }

        return winner is null ? null : ToSnapshot(winner);
    }
}
