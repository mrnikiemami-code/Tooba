#pragma warning disable CS1591
using Tooba.Payment.Application.Ports;
using Tooba.Payment.Contracts.Admin;
using Tooba.Payment.Contracts.Customer;
using Tooba.Payment.Contracts.Hold;

namespace Tooba.Payment.Infrastructure.Adapters;

/// <summary>
/// Contract bridge exposing Payment admin/customer/hold contract gateways. Expected failures originate as
/// <see cref="Tooba.BuildingBlocks.ContractOperationException"/> at owning Payment Directory;
/// this bridge does not parse Message or promote InvalidOperationException.
/// </summary>
public sealed class PaymentContractBridge(
    IPaymentDirectory payments,
    IPaymentAdminDirectory admin,
    IPaymentExpiryDirectory expiry,
    IPaymentHoldSettingsDirectory holds)
    : IPaymentAdminGateway, IPaymentCustomerGateway, IPaymentHoldSettingsGateway
{
    public async Task<PaymentAdminOperationalSnapshot?> GetLatestOperationalForCheckoutAsync(
        Guid checkoutId, CancellationToken cancellationToken)
    {
        var x = await admin.GetLatestOperationalForCheckoutAsync(checkoutId, cancellationToken);
        return x is null ? null : new(
            x.PaymentId, x.CheckoutId, x.Status.ToString(), x.Amount, x.Currency, x.ProviderCode,
            x.ProviderRequestReference, x.ProviderTransactionReference, x.CreatedAt, x.UpdatedAt,
            x.CompletedAt, x.LastFailureCode, x.ReconcileEligible, x.ConfirmDepositEligible,
            x.RejectDepositEligible, x.RestoreDepositEligible, x.HasManualDepositRejection,
            x.UnconfirmDepositEligible, x.CustomerTransferReference, x.ProofMediaAssetId,
            x.EvidenceSubmittedAt);
    }

    public Task CloseOrStartRefundForOrderCancelAsync(Guid checkoutId, CancellationToken cancellationToken) =>
        admin.CloseOrStartRefundForOrderCancelAsync(checkoutId, cancellationToken);

    public Task RestoreAfterOrderCancelRestoreAsync(Guid checkoutId, CancellationToken cancellationToken) =>
        admin.RestoreAfterOrderCancelRestoreAsync(checkoutId, cancellationToken);

    public async Task<PaymentAdminMutationResult> ConfirmDepositAsync(Guid paymentId, CancellationToken cancellationToken) =>
        Map(await admin.ConfirmDepositAsync(paymentId, cancellationToken));

    public async Task<PaymentAdminMutationResult> RejectDepositAsync(Guid paymentId, CancellationToken cancellationToken) =>
        Map(await admin.RejectDepositAsync(paymentId, cancellationToken));

    public async Task<PaymentAdminMutationResult> RestoreDepositAsync(Guid paymentId, CancellationToken cancellationToken) =>
        Map(await admin.RestoreDepositAsync(paymentId, cancellationToken));

    public async Task<PaymentAdminMutationResult> UnconfirmDepositAsync(Guid paymentId, CancellationToken cancellationToken) =>
        Map(await admin.UnconfirmDepositAsync(paymentId, cancellationToken));
    public async Task<PaymentCustomerSnapshot?> GetLatestForCheckoutAsync(
        Guid checkoutId, Guid actorUserId, Guid? buyerPartyId, CancellationToken cancellationToken)
    {
        var x = await payments.GetLatestForCheckoutAsync(checkoutId, actorUserId, buyerPartyId, cancellationToken);
        return x is null ? null : new(
            x.PaymentId, x.CheckoutId, x.Amount, x.Currency, x.Status.ToString(), x.ProviderCode,
            x.Allocations.Select(a => new PaymentCustomerAllocationSnapshot(
                a.SellerOrderId, a.AllocatedAmount, a.Currency)).ToArray(),
            x.CustomerTransferReference, x.ProofMediaAssetId, x.EvidenceSubmittedAt);
    }

    public Task<bool> HasSucceededPaymentForCheckoutAsync(Guid checkoutId, CancellationToken cancellationToken) =>
        payments.HasSucceededPaymentForCheckoutAsync(checkoutId, cancellationToken);

    public Task<IReadOnlyList<Guid>> ExpireDueUnpaidAsync(
        DateTimeOffset utcNow, int batchSize, CancellationToken cancellationToken) =>
        expiry.ExpireDueUnpaidAsync(utcNow, batchSize, cancellationToken);

    public Task ReopenExpiredForRetryAsync(
        Guid paymentId, Guid actorUserId, Guid? buyerPartyId, CancellationToken cancellationToken) =>
        expiry.ReopenExpiredForRetryAsync(paymentId, actorUserId, buyerPartyId, cancellationToken);

    public async Task<IReadOnlyList<PaymentMethodHoldOverride>> ListMethodOverridesAsync(
        CancellationToken cancellationToken) =>
        (await holds.ListMethodOverridesAsync(cancellationToken))
        .Select(x => new PaymentMethodHoldOverride(
            x.ProviderCode, x.OnlinePaymentHoldHours,
            x.ManualPaymentInitialHoldHours, x.ManualPaymentReviewHoldHours)).ToArray();

    public Task UpsertMethodOverrideAsync(
        string providerCode, int? onlinePaymentHoldHours, int? manualPaymentInitialHoldHours,
        int? manualPaymentReviewHoldHours, DateTimeOffset now, CancellationToken cancellationToken) =>
        holds.UpsertMethodOverrideAsync(
            providerCode, onlinePaymentHoldHours, manualPaymentInitialHoldHours,
            manualPaymentReviewHoldHours, now, cancellationToken);

    private static PaymentAdminMutationResult Map(PaymentVerificationResult x) =>
        new(x.PaymentId, x.Status.ToString(), x.NewlySucceeded);
}
