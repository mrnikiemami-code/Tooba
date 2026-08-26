using Microsoft.EntityFrameworkCore;
using Tooba.Payment.Application;
using Tooba.Payment.Domain;
using Tooba.Payment.Infrastructure.Persistence;

namespace Tooba.Payment.Infrastructure;

/// <summary>
/// نگهبان باز موردکاربرد Payment. شمارهٔ پرداخت به‌تنهایی اجازهٔ جهش نیست.
/// </summary>
public sealed class OpenPaymentUseCaseGuard : IPaymentUseCaseGuard
{
    /// <inheritdoc />
    public Task EnsureCanMutateAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}

/// <summary>
/// ارکستراسیون پرداخت در schema payment. مبلغ از تصویر سفارش است نه از کلاینت؛ OrderDbContext اینجا باز نمی‌شود.
/// </summary>
public sealed class PaymentDirectory : IPaymentDirectory, IPaymentReconciliationDirectory
{
    private readonly PaymentDbContext _db;
    private readonly IPaymentUseCaseGuard _guard;
    private readonly IPayableCheckoutReader _orders;
    private readonly IPaymentGatewayRegistry _gateways;

    /// <summary>
    /// دایرکتوری را به schema payment و رجیستری درگاه وصل می‌کند. تصویر Paid سفارش از Outbox می‌آید نه از همین تراکنش.
    /// </summary>
    public PaymentDirectory(
        PaymentDbContext db,
        IPaymentUseCaseGuard guard,
        IPayableCheckoutReader orders,
        IPaymentGatewayRegistry gateways)
    {
        _db = db;
        _guard = guard;
        _orders = orders;
        _gateways = gateways;
    }

    /// <inheritdoc />
    public async Task<PaymentInitiationResult> InitiateAsync(InitiatePaymentCommand command, CancellationToken cancellationToken)
    {
        await _guard.EnsureCanMutateAsync(cancellationToken);
        var key = command.IdempotencyKey.Trim();
        var existing = await _db.Payments.AsNoTracking().FirstOrDefaultAsync(x => x.IdempotencyKey == key, cancellationToken);
        if (existing is not null)
        {
            await EnsureActorCanSeeAsync(existing, command.ActorUserId, command.BuyerPartyId, cancellationToken);
            var prior = await _db.Attempts
                .AsNoTracking()
                .OrderByDescending(x => x.CreatedAt)
                .FirstAsync(x => x.PaymentId == existing.PaymentId, cancellationToken);
            var replayGateway = _gateways.Resolve(existing.ProviderCode);
            var replayInitiation = await replayGateway.InitiateAsync(
                existing.PaymentId,
                existing.Amount,
                existing.Currency,
                cancellationToken);
            return new PaymentInitiationResult(
                existing.PaymentId,
                prior.AttemptId,
                existing.Status,
                existing.ProviderCode,
                prior.ProviderRequestReference,
                ResolveRedirectUrl(replayGateway, existing.PaymentId, prior.AttemptId, prior.ProviderRequestReference, replayInitiation.RedirectUrl),
                existing.Amount,
                existing.Currency);
        }

        var payable = await _orders.GetPayableAsync(command.CheckoutId, command.ActorUserId, command.BuyerPartyId, cancellationToken)
            ?? throw new InvalidOperationException("checkout قابل پرداخت پیدا نشد.");
        if (payable.Mode != OrderPaymentMode.OnlinePurchase)
        {
            throw new InvalidOperationException("درخواست رزرو در ثبت اولیه پرداخت نمی‌خواهد.");
        }

        var pending = payable.SellerOrders.Where(x => x.PendingPayment).ToArray();
        if (pending.Length == 0)
        {
            throw new InvalidOperationException("این سفارش قبلاً پرداخت شده است.");
        }

        if (pending.Select(x => x.Currency).Distinct(StringComparer.OrdinalIgnoreCase).Count() != 1
            || !string.Equals(payable.Currency, pending[0].Currency, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("ارز پرداخت باید با تصویر سفارش یکی باشد؛ تبدیل ارز در Payment نیست.");
        }

        var amount = pending.Sum(x => x.PayableAmount);
        var gateway = _gateways.Resolve(command.ProviderCode);
        var payment = CustomerPayment.Open(
            payable.CheckoutId,
            amount,
            payable.Currency,
            gateway.ProviderCode,
            key,
            pending.Select(x => (x.SellerOrderId, x.PayableAmount)).ToArray(),
            DateTimeOffset.UtcNow);
        var initiation = await gateway.InitiateAsync(payment.PaymentId, payment.Amount, payment.Currency, cancellationToken);
        var attempt = payment.RecordInitiation(initiation.ProviderRequestReference, DateTimeOffset.UtcNow);
        _db.Payments.Add(payment);
        _db.Allocations.AddRange(payment.Allocations);
        _db.Attempts.Add(attempt);
        await _db.SaveChangesAsync(cancellationToken);
        return new PaymentInitiationResult(
            payment.PaymentId,
            attempt.AttemptId,
            payment.Status,
            payment.ProviderCode,
            attempt.ProviderRequestReference,
            ResolveRedirectUrl(gateway, payment.PaymentId, attempt.AttemptId, attempt.ProviderRequestReference, initiation.RedirectUrl),
            payment.Amount,
            payment.Currency);
    }

    /// <inheritdoc />
    public async Task<int> ReconcileStalePendingAsync(
        DateTimeOffset asOf,
        TimeSpan minAge,
        int batchSize,
        CancellationToken cancellationToken)
    {
        var cutoff = asOf - minAge;
        var pending = await _db.Payments.AsNoTracking()
            .Where(x => x.Status == PaymentStatus.Pending && x.UpdatedAt <= cutoff)
            .OrderBy(x => x.UpdatedAt)
            .Take(Math.Max(1, batchSize))
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var processed = 0;
        foreach (var payment in pending)
        {
            var attempt = await _db.Attempts.AsNoTracking()
                .Where(x => x.PaymentId == payment.PaymentId)
                .OrderByDescending(x => x.CreatedAt)
                .FirstOrDefaultAsync(cancellationToken)
                .ConfigureAwait(false);
            if (attempt is null)
            {
                continue;
            }

            await VerifyAsync(
                new VerifyPaymentCommand(
                    payment.PaymentId,
                    attempt.AttemptId,
                    attempt.ProviderRequestReference,
                    false),
                cancellationToken).ConfigureAwait(false);
            processed++;
        }

        return processed;
    }

    /// <inheritdoc />
    public async Task<PaymentVerificationResult> VerifyAsync(VerifyPaymentCommand command, CancellationToken cancellationToken)
    {
        await _guard.EnsureCanMutateAsync(cancellationToken);
        var payment = await _db.Payments.SingleOrDefaultAsync(x => x.PaymentId == command.PaymentId, cancellationToken)
            ?? throw new InvalidOperationException("پرداخت پیدا نشد.");
        var attempt = await _db.Attempts.SingleOrDefaultAsync(
            x => x.AttemptId == command.AttemptId && x.PaymentId == payment.PaymentId,
            cancellationToken)
            ?? throw new InvalidOperationException("تلاش پرداخت پیدا نشد.");
        if (attempt.ProviderRequestReference != command.ProviderRequestReference)
        {
            throw new InvalidOperationException("مرجع درگاه با تلاش ذخیره‌شده یکی نیست.");
        }

        if (payment.Status == PaymentStatus.Succeeded)
        {
            return new PaymentVerificationResult(payment.PaymentId, payment.Status, false);
        }

        var gateway = _gateways.Resolve(payment.ProviderCode);
        var verified = await gateway.VerifyAsync(command.ProviderRequestReference, command.CallbackClaimsSuccess, cancellationToken);
        payment.AttachLoadedAttempt(attempt);
        var allocations = await _db.Allocations.Where(x => x.PaymentId == payment.PaymentId).ToListAsync(cancellationToken);
        payment.AttachLoadedAllocations(allocations);
        if (!verified.VerifiedSuccess || string.IsNullOrWhiteSpace(verified.ProviderTransactionReference))
        {
            payment.ApplyVerifiedFailure(attempt.AttemptId, verified.FailureCode, DateTimeOffset.UtcNow);
            await _db.SaveChangesAsync(cancellationToken);
            return new PaymentVerificationResult(payment.PaymentId, payment.Status, false);
        }

        var duplicateTxn = await _db.Attempts.AnyAsync(
            x => x.ProviderTransactionReference == verified.ProviderTransactionReference,
            cancellationToken);
        if (duplicateTxn)
        {
            return new PaymentVerificationResult(payment.PaymentId, payment.Status, false);
        }

        var firstSuccess = payment.ApplyVerifiedSuccess(attempt.AttemptId, verified.ProviderTransactionReference, DateTimeOffset.UtcNow);
        await _db.SaveChangesAsync(cancellationToken);
        return new PaymentVerificationResult(payment.PaymentId, payment.Status, firstSuccess);
    }

    /// <inheritdoc />
    public async Task<PaymentSnapshot?> GetAsync(Guid paymentId, Guid actorUserId, Guid? buyerPartyId, CancellationToken cancellationToken)
    {
        var payment = await _db.Payments.SingleOrDefaultAsync(x => x.PaymentId == paymentId, cancellationToken);
        if (payment is null)
        {
            return null;
        }

        await EnsureActorCanSeeAsync(payment, actorUserId, buyerPartyId, cancellationToken);
        var allocations = await _db.Allocations.Where(x => x.PaymentId == paymentId).ToListAsync(cancellationToken);
        return new PaymentSnapshot(
            payment.PaymentId,
            payment.CheckoutId,
            payment.Amount,
            payment.Currency,
            payment.Status,
            payment.ProviderCode,
            allocations.Select(x => new PaymentAllocationSnapshot(x.SellerOrderId, x.AllocatedAmount, x.Currency)).ToArray());
    }

    /// <inheritdoc />
    public async Task<PaymentSnapshot?> GetLatestForCheckoutAsync(
        Guid checkoutId,
        Guid actorUserId,
        Guid? buyerPartyId,
        CancellationToken cancellationToken)
    {
        var payable = await _orders.GetPayableAsync(checkoutId, actorUserId, buyerPartyId, cancellationToken);
        if (payable is null)
        {
            return null;
        }

        var payment = await _db.Payments.AsNoTracking()
            .Where(x => x.CheckoutId == checkoutId)
            .OrderByDescending(x => x.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);
        if (payment is null)
        {
            return null;
        }

        var allocations = await _db.Allocations.AsNoTracking()
            .Where(x => x.PaymentId == payment.PaymentId)
            .ToListAsync(cancellationToken);
        return new PaymentSnapshot(
            payment.PaymentId,
            payment.CheckoutId,
            payment.Amount,
            payment.Currency,
            payment.Status,
            payment.ProviderCode,
            allocations.Select(x => new PaymentAllocationSnapshot(x.SellerOrderId, x.AllocatedAmount, x.Currency)).ToArray());
    }

    private async Task EnsureActorCanSeeAsync(
        CustomerPayment payment,
        Guid actorUserId,
        Guid? buyerPartyId,
        CancellationToken cancellationToken)
    {
        var payable = await _orders.GetPayableAsync(payment.CheckoutId, actorUserId, buyerPartyId, cancellationToken);
        if (payable is null)
        {
            throw new InvalidOperationException("دسترسی به پرداخت بدون هویت سفارش رد شد.");
        }
    }

    private static string ResolveRedirectUrl(
        IPaymentGateway gateway,
        Guid paymentId,
        Guid attemptId,
        string providerRequestReference,
        string? gatewayRedirect)
    {
        if (!string.IsNullOrWhiteSpace(gatewayRedirect))
        {
            return gatewayRedirect;
        }

        if (gateway is FakePaymentGateway or FakeFailingPaymentGateway)
        {
            return ComposeSandboxRedirect(paymentId, attemptId, providerRequestReference);
        }

        return "/payment/result?paymentId=" + paymentId.ToString("D")
            + "&attemptId=" + attemptId.ToString("D")
            + "&ref=" + Uri.EscapeDataString(providerRequestReference);
    }

    private static string ComposeSandboxRedirect(Guid paymentId, Guid attemptId, string providerRequestReference) =>
        "/payment/sandbox?paymentId=" + paymentId.ToString("D")
        + "&attemptId=" + attemptId.ToString("D")
        + "&ref=" + Uri.EscapeDataString(providerRequestReference);
}
