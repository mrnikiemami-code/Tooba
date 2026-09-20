using System.Transactions;
using Microsoft.EntityFrameworkCore;
using Tooba.Cart.Application;
using Tooba.Cart.Domain;
using Tooba.Order.Application;
using Tooba.BuildingBlocks;
using Tooba.Order.Domain;

namespace Tooba.Order.Infrastructure;

/// <summary>اجرای SubmitAsync با ردیابی فرآیند W1؛ TransactionScope مشترک حفظ می‌شود.</summary>
internal static class CheckoutSubmitExecutor
{
    public static async Task<CheckoutSnapshot> ExecuteAsync(CheckoutDirectory d, SubmitCheckoutCommand command, CancellationToken cancellationToken)
    {
        await d._guard.EnsureCanMutateAsync(cancellationToken);
        var existing = await d.FindCheckoutAsync(
            x => x.IdempotencyKey == command.IdempotencyKey.Trim() || x.CartId == command.CartId,
            cancellationToken);
        if (existing is not null)
        {
            CheckoutDirectory.EnsureAccess(existing, new OrderAccess(command.BuyerPartyId, command.PlacedByUserId));
            await d.ReconcileCartConversionAsync(existing, command, cancellationToken);
            return CheckoutDirectory.ToSnapshot(existing);
        }

        var cart = await d._carts.GetCartAsync(command.CartId, command.CartAccess, cancellationToken)
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

        var now = DateTimeOffset.UtcNow;
        var checkoutId = UuidV7.New();
        CheckoutProcess? process = null;
        if (d._processes is not null)
        {
            process = await d._processes.BeginAsync(command.IdempotencyKey, cart.CartId, now, cancellationToken);
            if (process.IsSubmitSucceeded && process.CheckoutId is { } existingCheckoutId)
            {
                var prior = await d.FindCheckoutAsync(x => x.CheckoutId == existingCheckoutId, cancellationToken);
                if (prior is not null)
                {
                    CheckoutDirectory.EnsureAccess(prior, new OrderAccess(command.BuyerPartyId, command.PlacedByUserId));
                    return CheckoutDirectory.ToSnapshot(prior);
                }
            }

            d._processes.MarkValidating(process, now);
        }

        var sellerOrders = await d.QuoteSellerOrdersAsync(
            cart,
            command,
            checkoutId,
            now,
            new Dictionary<Guid, Guid>(),
            requireReservation: false,
            cancellationToken);

        Dictionary<Guid, Guid>? reservations = null;
        var abuseSettings = await d._abuseGate.LoadSettingsAsync(cancellationToken);
        try
        {
            using var scope = new TransactionScope(
                TransactionScopeOption.Required,
                new TransactionOptions { IsolationLevel = IsolationLevel.ReadCommitted },
                TransactionScopeAsyncFlowOption.Enabled);

            await d._abuseGate.EnsureCanStartInitialReservationAsync(
                command.PlacedByUserId,
                abuseSettings,
                cancellationToken);
            if (process is not null)
            {
                d._processes!.MarkInventoryReserving(process, now);
            }

            reservations = await d.ReserveCartLinesForOrderAsync(cart, now, cancellationToken);
            await d._commitBarrier.OnAfterReserveAsync(cancellationToken);

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
            CheckoutDirectory.BindReservationsToOrders(group, cart, reservations);
            if (process is not null)
            {
                d._processes!.MarkOrderPersisting(process, group.CheckoutId, now);
            }

            d._db.Checkouts.Add(group);
            await d.PrepareInitialCycleAsync(group, command.Mode, reservations.Values, now, cancellationToken);
            var primaryOrderId = group.SellerOrders.Select(x => x.SellerOrderId).First();
            d._abuseGate.PrepareInitialCommit(
                command.PlacedByUserId,
                group.CheckoutId,
                primaryOrderId,
                abuseSettings,
                now);
            try
            {
                await d._db.SaveChangesAsync(cancellationToken);
            }
            catch (DbUpdateException)
            {
                d._db.ChangeTracker.Clear();
                throw new InvalidOperationException("checkout.conflict");
            }

            await d._commitBarrier.OnAfterOrderWriteAsync(cancellationToken);

            if (process is not null)
            {
                d._processes!.MarkCartCommitting(process, now);
                await d._db.SaveChangesAsync(cancellationToken);
            }

            var intent = command.Mode == OrderMode.RequestToReserve
                ? CartConversionIntent.RequestToReserve
                : CartConversionIntent.OnlinePurchase;
            try
            {
                await d._cartMutations.ConvertAsync(group.CartId, command.CartAccess, cart.Version, intent, cancellationToken);
            }
            catch (InvalidOperationException)
            {
                var latestCart = await d._carts.GetCartAsync(group.CartId, command.CartAccess, cancellationToken);
                if (latestCart?.Status != CartStatus.Converted)
                {
                    throw;
                }

                // رقیب همزمان سبد را تبدیل کرده؛ سفارش ما از قبل ذخیره شده است.
                d._db.ChangeTracker.Clear();
                var raced = await d._db.Checkouts
                    .AsNoTracking()
                    .Include(x => x.SellerOrders)
                    .ThenInclude(x => x.Lines)
                    .Where(x => x.CartId == command.CartId)
                    .OrderBy(x => x.SubmittedAt)
                    .FirstOrDefaultAsync(cancellationToken);
                if (raced is null)
                {
                    throw;
                }

                CheckoutDirectory.EnsureAccess(raced, new OrderAccess(command.BuyerPartyId, command.PlacedByUserId));
                if (process is not null)
                {
                    d._processes!.MarkPaymentPending(process, now);
                    await d._db.SaveChangesAsync(cancellationToken);
                }

                scope.Complete();
                return CheckoutDirectory.ToSnapshot(raced);
            }

            await d._commitBarrier.OnAfterCartConvertedWriteAsync(cancellationToken);
            if (process is not null)
            {
                d._processes!.MarkPaymentPending(process, now);
                await d._db.SaveChangesAsync(cancellationToken);
            }

            scope.Complete();
            return CheckoutDirectory.ToSnapshot(group);
        }
        catch (InvalidOperationException ex) when (ex.Message is "checkout.conflict" or "inventory.reservation.conflict")
        {
            d._db.ChangeTracker.Clear();
            await d._db.Database.CloseConnectionAsync();
            CheckoutGroup? winner = null;
            for (var attempt = 0; attempt < 10 && winner is null; attempt++)
            {
                if (attempt > 0)
                {
                    await Task.Delay(20 * attempt, cancellationToken);
                }

                winner = await d._db.Checkouts
                    .AsNoTracking()
                    .Include(x => x.SellerOrders)
                    .ThenInclude(x => x.Lines)
                    .Where(x => x.CartId == command.CartId)
                    .OrderBy(x => x.SubmittedAt)
                    .FirstOrDefaultAsync(cancellationToken);
            }

            if (winner is null)
            {
                if (ex.Message == "inventory.reservation.conflict")
                {
                    throw;
                }

                throw new InvalidOperationException("checkout تکراری سبد ذخیره شد ولی خوانده نشد.");
            }

            CheckoutDirectory.EnsureAccess(winner, new OrderAccess(command.BuyerPartyId, command.PlacedByUserId));
            await d.ReconcileCartConversionAsync(winner, command, cancellationToken);
            return CheckoutDirectory.ToSnapshot(winner);
        }
        catch
        {
            if (reservations is { Count: > 0 })
            {
                try
                {
                    await d.ReleaseAcquiredAsync(reservations.Values, cancellationToken);
                }
                catch (InvalidOperationException)
                {
                }
            }

            throw;
        }
    }
}