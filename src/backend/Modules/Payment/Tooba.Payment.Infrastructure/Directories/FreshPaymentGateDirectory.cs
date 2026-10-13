using Microsoft.EntityFrameworkCore;
using Tooba.BuildingBlocks;
using Tooba.Order.Contracts.Payments;
using Tooba.Payment.Application.Models;
using Tooba.Payment.Application.Ports;
using Tooba.Payment.Contracts.Checkout;
using Tooba.Payment.Contracts.Errors;
using Tooba.Payment.Domain.ValueObjects;
using Tooba.Payment.Infrastructure.Directories.Shared;
using Tooba.Payment.Infrastructure.Persistence;

namespace Tooba.Payment.Infrastructure.Directories;

/// <summary>
/// Mandatory Payment-owned choke point in front of <see cref="IPaymentDirectory"/> for the canonical
/// fresh-money entry. It refuses a NEW money movement whose Order-side reservation truth is not provably
/// payable RIGHT NOW, using the accepted <see cref="IOrderPaymentFreshPayabilityPort"/> (task 024), and
/// never weakens anything the inner directory owns.
/// <para>
/// Why a decorator rather than an extra constructor dependency on <c>PaymentDirectory</c>: that class is a
/// byte/loc-line frozen certified artifact and its legacy construction sites are historical fixtures. The
/// decorator is the single registered <see cref="IPaymentDirectory"/>, so the composed application cannot
/// bypass it; a hand-constructed directory in a historical test is the only non-composed path.
/// </para>
/// <para>
/// Ordering discipline (deliberately mirroring the inner directory, which stays authoritative):
/// <list type="number">
/// <item>An existing key is resolved first and the inner access rule is applied through the very same
/// shared <c>PaymentActorAccess</c>, so actor/guest visibility still precedes any refusal.</item>
/// <item>A reused key that no longer describes this exact operation (another checkout, or another
/// canonical provider) is left entirely to the inner directory so the 021 cross-checkout/cross-method
/// idempotency conflict keeps winning.</item>
/// <item>A checkout that already has a succeeded payment stays terminal (inner emits
/// <c>payment.already_succeeded</c>) and is never masked by the reservation verdict.</item>
/// <item>Only the two paths that WOULD call a provider are gated: no persisted key (fresh), and a
/// same-operation <c>Failed</c> prior that is about to be re-initiated. Pending/Succeeded identical-key
/// replay is delegated untouched and never re-checked against an expired cycle.</item>
/// </list>
/// </para>
/// <para>
/// NOT claimed: this point-in-time read is not an atomic stock promise. A cycle or hold can expire or be
/// released immediately after the verdict; closing that residual window belongs to the canonical
/// reacquire/refund path, not here. Payment retry is never confused with reservation-cycle renewal.
/// </para>
/// </summary>
public sealed class FreshPaymentGateDirectory : IPaymentDirectory
{
    private readonly IPaymentDirectory _inner;
    private readonly PaymentDbContext _db;
    private readonly PaymentActorAccess _actorAccess;
    private readonly IPaymentGatewayRegistry _gateways;
    private readonly IOrderPaymentFreshPayabilityPort _fresh;

    /// <summary>دایرکتوری را با گیت فقط‌خواندنی «پول تازه» می‌پوشاند؛ پورت سفارش یک وابستگی الزامی است.</summary>
    public FreshPaymentGateDirectory(
        IPaymentDirectory inner,
        PaymentDbContext db,
        IPayableCheckoutReader orders,
        IPaymentGatewayRegistry gateways,
        IOrderPaymentFreshPayabilityPort fresh)
    {
        _inner = inner;
        _db = db;
        _actorAccess = new PaymentActorAccess(orders);
        _gateways = gateways;
        _fresh = fresh;
    }

    /// <inheritdoc />
    public async Task<PaymentInitiationResult> InitiateAsync(
        InitiatePaymentCommand command,
        CancellationToken cancellationToken)
    {
        var key = command.IdempotencyKey.Trim();
        var persisted = await _db.Payments
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.IdempotencyKey == key, cancellationToken)
            .ConfigureAwait(false);

        if (persisted is null)
        {
            if (!await _inner.HasSucceededPaymentForCheckoutAsync(command.CheckoutId, cancellationToken).ConfigureAwait(false))
            {
                await EnsureFreshPayableAsync(command.CheckoutId, cancellationToken).ConfigureAwait(false);
            }
        }
        else
        {
            await _actorAccess
                .EnsureActorCanSeeAsync(persisted, command.ActorUserId, command.BuyerPartyId, cancellationToken)
                .ConfigureAwait(false);
            var requestedProviderCode = _gateways.Resolve(command.ProviderCode).ProviderCode;
            var sameOperation = persisted.CheckoutId == command.CheckoutId
                && string.Equals(requestedProviderCode, persisted.ProviderCode, StringComparison.OrdinalIgnoreCase);
            if (sameOperation
                && persisted.Status == PaymentStatus.Failed
                && !await _inner.HasSucceededPaymentForCheckoutAsync(persisted.CheckoutId, cancellationToken).ConfigureAwait(false))
            {
                await EnsureFreshPayableAsync(persisted.CheckoutId, cancellationToken).ConfigureAwait(false);
            }
        }

        return await _inner.InitiateAsync(command, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public Task<PaymentVerificationResult> VerifyAsync(
        VerifyPaymentCommand command,
        CancellationToken cancellationToken) =>
        _inner.VerifyAsync(command, cancellationToken);

    /// <inheritdoc />
    public Task<PaymentSnapshot?> GetAsync(
        Guid paymentId,
        Guid actorUserId,
        Guid? buyerPartyId,
        CancellationToken cancellationToken) =>
        _inner.GetAsync(paymentId, actorUserId, buyerPartyId, cancellationToken);

    /// <inheritdoc />
    public Task<PaymentSnapshot?> GetLatestForCheckoutAsync(
        Guid checkoutId,
        Guid actorUserId,
        Guid? buyerPartyId,
        CancellationToken cancellationToken) =>
        _inner.GetLatestForCheckoutAsync(checkoutId, actorUserId, buyerPartyId, cancellationToken);

    /// <inheritdoc />
    public Task<bool> HasSucceededPaymentForCheckoutAsync(Guid checkoutId, CancellationToken cancellationToken) =>
        _inner.HasSucceededPaymentForCheckoutAsync(checkoutId, cancellationToken);

    /// <inheritdoc />
    public Task RegisterProofAssetAsync(
        Guid paymentId,
        Guid actorUserId,
        Guid? buyerPartyId,
        Guid mediaAssetId,
        CancellationToken cancellationToken) =>
        _inner.RegisterProofAssetAsync(paymentId, actorUserId, buyerPartyId, mediaAssetId, cancellationToken);

    /// <inheritdoc />
    public Task SubmitManualEvidenceAsync(
        Guid paymentId,
        Guid actorUserId,
        Guid? buyerPartyId,
        string transferReference,
        Guid? proofMediaAssetId,
        CancellationToken cancellationToken) =>
        _inner.SubmitManualEvidenceAsync(
            paymentId,
            actorUserId,
            buyerPartyId,
            transferReference,
            proofMediaAssetId,
            cancellationToken);

    /// <inheritdoc />
    public Task RetryManualAfterRejectionAsync(
        Guid paymentId,
        Guid actorUserId,
        Guid? buyerPartyId,
        CancellationToken cancellationToken) =>
        _inner.RetryManualAfterRejectionAsync(paymentId, actorUserId, buyerPartyId, cancellationToken);

    private async Task EnsureFreshPayableAsync(Guid checkoutId, CancellationToken cancellationToken)
    {
        var verdict = await _fresh
            .GetFreshPaymentPayabilityAsync(checkoutId, cancellationToken)
            .ConfigureAwait(false);
        if (!verdict.Payable)
        {
            throw new ContractOperationException(PaymentErrorCodes.ReservationNotPayable);
        }
    }
}
