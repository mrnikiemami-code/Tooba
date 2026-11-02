using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Tooba.Order.Contracts.Shipping;
using Tooba.Shipping.Application.Authoring.Models;
using Tooba.Shipping.Application.Carriers;
using Tooba.Shipping.Application.Carriers.Ports;
using Tooba.Shipping.Contracts.Carriers;
using Tooba.Shipping.Contracts.Errors;
using Tooba.Shipping.Domain.Authoring;
using Tooba.Shipping.Domain.Carriers;
using Tooba.Shipping.Infrastructure.Carriers.Tipax;
using Tooba.Shipping.Infrastructure.Persistence;

namespace Tooba.Shipping.Infrastructure.Carriers;

internal sealed class ShippingCarrierExecutionReadiness(
    ShippingDbContext db,
    IShippingCarrierAccountDirectory accounts,
    ICarrierShipmentProviderRegistry providers,
    IOptionsMonitor<TipaxV4Options> tipaxOptions) : IShippingCarrierExecutionReadiness
{
    public async Task<ShippingGroupCarrierExecutionCapabilityView> BuildCapabilityAsync(
        ShippingAuthoringOwnerScope owner,
        OrderCommittedCarrierShipmentFacts facts,
        CancellationToken cancellationToken)
    {
        var snapshot = await EvaluateAsync(owner, facts, cancellationToken);
        return MapView(facts, snapshot);
    }

    public async Task<(bool Allowed, string? FailureCode, ShippingCarrierShipmentPartyInput? Sender, ShippingCarrierShipmentPackageInput? Package)>
        EvaluateCreateAsync(
            ShippingAuthoringOwnerScope owner,
            OrderCommittedCarrierShipmentFacts facts,
            ShippingCarrierShipmentPartyInput requestedSender,
            CancellationToken cancellationToken)
    {
        if (!ShippingCarrierOwnerScopeMatching.Matches(owner, facts))
        {
            return (false, ShippingErrorCodes.CarrierShipmentNotEligible, null, null);
        }

        var requestedLocation = (requestedSender.LocationCode ?? string.Empty).Trim();
        if (requestedLocation.Length > 0
            && !string.Equals(requestedLocation, facts.OriginLocationCode, StringComparison.Ordinal))
        {
            return (false, ShippingErrorCodes.CarrierShipmentRequestInvalid, null, null);
        }

        var snapshot = await EvaluateAsync(owner, facts, cancellationToken);
        if (snapshot.BlockReasonCodes.Contains("account_unconfigured", StringComparer.Ordinal)
            || snapshot.BlockReasonCodes.Contains("account_inactive", StringComparer.Ordinal))
        {
            return (false, ShippingErrorCodes.CarrierUnconfigured, null, null);
        }

        if (snapshot.BlockReasonCodes.Any(code => !string.Equals(code, "sender_incomplete", StringComparison.Ordinal))
            || !snapshot.PackageReady
            || snapshot.LatestCreateOperation is not null)
        {
            return (false, ShippingErrorCodes.CarrierShipmentNotEligible, null, null);
        }

        if (!snapshot.CanCreate && snapshot.BlockReasonCodes.All(code => !string.Equals(code, "sender_incomplete", StringComparison.Ordinal)))
        {
            return (false, ShippingErrorCodes.CarrierShipmentNotEligible, null, null);
        }

        var sender = ResolveSenderForCreate(snapshot, facts, requestedSender);
        if (sender is null)
        {
            return (false, ShippingErrorCodes.CarrierShipmentNotEligible, null, null);
        }

        var package = snapshot.EffectivePackage;
        if (package is null)
        {
            return (false, ShippingErrorCodes.CarrierShipmentNotEligible, null, null);
        }

        return (true, null, sender, package);
    }

    private async Task<ReadinessSnapshot> EvaluateAsync(
        ShippingAuthoringOwnerScope owner,
        OrderCommittedCarrierShipmentFacts facts,
        CancellationToken cancellationToken)
    {
        var reasons = new List<string>();
        void Add(string code)
        {
            if (!reasons.Contains(code, StringComparer.Ordinal))
            {
                reasons.Add(code);
            }
        }

        if (!string.IsNullOrWhiteSpace(facts.IneligibilityReasonCode))
        {
            Add(facts.IneligibilityReasonCode);
        }

        // Order-side eligibility: Cancelled and payment lock only (same set as AdminFulfillmentCapabilityProjector).
        if (facts.IsCancelled)
        {
            Add("cancelled");
        }

        if (facts.PaymentLocked)
        {
            Add("payment_locked");
        }

        if (!string.Equals(facts.TransportMode, "CarrierApi", StringComparison.Ordinal))
        {
            Add(facts.HasCarrierQuote ? "not_carrier_api" : "legacy_incomplete");
        }

        var domainOwner = ToDomainOwner(owner);
        ShippingCarrierAccountExecutionContext? context = null;
        CarrierShipmentCapabilities caps = new("unknown", false, false, false, false);
        if (facts.CarrierAccountId is { } accountId)
        {
            context = await accounts.TryResolveExecutionContextAsync(
                domainOwner.Kind,
                domainOwner.TenantId,
                domainOwner.SellerPartyId,
                accountId,
                cancellationToken);
            if (context is null || string.IsNullOrWhiteSpace(context.Username))
            {
                Add("account_unconfigured");
            }
            else if (!context.IsActive)
            {
                Add("account_inactive");
            }
            else if (facts.ConfigurationVersion is { } expected
                     && context.ConfigurationVersion != expected)
            {
                Add("config_reconciliation_required");
            }
            else if (context.MethodBinding is null
                     || string.IsNullOrWhiteSpace(context.MethodBinding.OriginExternalCityCode))
            {
                Add("bindings_incomplete");
            }

            if (providers.TryResolve(context?.ProviderCode ?? facts.ProviderCode ?? string.Empty, out var provider)
                && provider is not null)
            {
                caps = provider.Capabilities;
            }
        }
        else if (facts.HasCarrierQuote)
        {
            Add("account_unconfigured");
        }

        var options = tipaxOptions.CurrentValue;
        if (facts.HasCarrierQuote
            && (!Uri.TryCreate(options.SuccessCallbackUrl, UriKind.Absolute, out _)
                || !Uri.TryCreate(options.FailureCallbackUrl, UriKind.Absolute, out _)))
        {
            Add("callbacks_missing");
        }

        var packageReady = HasFullPackageFromFacts(facts);
        ShippingCarrierShipmentPackageCompletion? packageCompletion = null;
        if (!packageReady && facts.HasCarrierQuote)
        {
            packageCompletion = await LoadPackageCompletionAsync(domainOwner, facts, cancellationToken);
            packageReady = packageCompletion is not null;
            if (!packageReady)
            {
                Add("package_completion_required");
            }
        }

        var senderCompletion = await LoadSenderCompletionAsync(domainOwner, facts, cancellationToken);
        var configuredSender = BuildConfiguredSender(context, facts, senderCompletion);
        var senderComplete = IsSenderComplete(configuredSender);
        if (facts.HasCarrierQuote && string.Equals(facts.TransportMode, "CarrierApi", StringComparison.Ordinal) && !senderComplete)
        {
            Add("sender_incomplete");
        }

        var operationKey = CreateOperationKey(facts.CheckoutId, facts.ShippingGroupId);
        var createOp = await db.CarrierShipmentOperations.AsNoTracking()
            .SingleOrDefaultAsync(x => x.OperationKey == operationKey && x.Kind == ShippingCarrierShipmentKind.Create, cancellationToken);

        var hardReasons = reasons.Where(r => !string.Equals(r, "sender_incomplete", StringComparison.Ordinal)).ToList();
        var canCreate = hardReasons.Count == 0
                        && facts.HasCarrierQuote
                        && string.Equals(facts.TransportMode, "CarrierApi", StringComparison.Ordinal)
                        && packageReady
                        && createOp is null
                        && caps.CanCreate;

        if (createOp is not null)
        {
            canCreate = false;
            switch (createOp.State)
            {
                case ShippingCarrierShipmentState.Pending:
                    Add("operation_pending");
                    break;
                case ShippingCarrierShipmentState.Succeeded:
                    Add("operation_succeeded");
                    break;
                case ShippingCarrierShipmentState.Unknown:
                    Add("operation_unknown");
                    break;
            }
        }

        var canTrack = false;
        var canCancel = false;
        var canLabel = caps.CanLabel;
        if (createOp is not null)
        {
            canTrack = caps.CanTrack && createOp.State is ShippingCarrierShipmentState.Succeeded or ShippingCarrierShipmentState.Unknown;
            canCancel = caps.CanCancel
                        && createOp.State == ShippingCarrierShipmentState.Succeeded
                        && !string.IsNullOrWhiteSpace(createOp.ProviderTrackingCode);
        }

        if (!context?.IsActive ?? false)
        {
            canTrack = false;
            canCancel = false;
        }

        ShippingCarrierShipmentPackageInput? effectivePackage = null;
        if (packageReady)
        {
            if (HasFullPackageFromFacts(facts))
            {
                effectivePackage = new ShippingCarrierShipmentPackageInput(
                    facts.WeightGrams!.Value,
                    facts.DeclaredValue ?? 0m,
                    facts.LengthCentimeters!.Value,
                    facts.WidthCentimeters!.Value,
                    facts.HeightCentimeters!.Value,
                    facts.CarrierCollectAmount);
            }
            else if (packageCompletion is not null)
            {
                effectivePackage = new ShippingCarrierShipmentPackageInput(
                    packageCompletion.WeightGrams,
                    packageCompletion.DeclaredValue,
                    packageCompletion.LengthCentimeters,
                    packageCompletion.WidthCentimeters,
                    packageCompletion.HeightCentimeters,
                    facts.CarrierCollectAmount);
            }
        }

        return new ReadinessSnapshot(
            canCreate,
            canTrack,
            canCancel,
            canLabel,
            reasons,
            reasons.Count == 0 ? facts.IneligibilityReasonCode : reasons[0],
            configuredSender,
            packageReady,
            facts.HasCarrierQuote && !HasFullPackageFromFacts(facts),
            !senderComplete && string.IsNullOrWhiteSpace(configuredSender.PostalCode),
            !senderComplete && string.IsNullOrWhiteSpace(configuredSender.Mobile),
            createOp is null
                ? null
                : new ShippingCarrierLatestCreateOperationView(
                    createOp.OperationId,
                    (ShippingCarrierShipmentStateView)createOp.State,
                    createOp.ProviderTrackingCode),
            effectivePackage);
    }

    private static ShippingGroupCarrierExecutionCapabilityView MapView(
        OrderCommittedCarrierShipmentFacts facts,
        ReadinessSnapshot snapshot)
        => new(
            facts.CheckoutId,
            facts.ShippingGroupId,
            facts.SellerPartyId,
            facts.SellerOrderId,
            facts.MethodCode,
            facts.MethodLabel,
            facts.TransportMode,
            facts.OwnerKind,
            facts.OwnerTenantKey,
            facts.OwnerSellerPartyId,
            facts.RecipientName,
            facts.RecipientMobile,
            facts.RecipientAddress,
            facts.PostalCode,
            facts.ProvinceName,
            facts.CityName,
            facts.Currency,
            facts.PayableAtCheckoutAmount,
            facts.CarrierCollectAmount,
            facts.HasCarrierQuote,
            facts.CarrierAccountId,
            facts.ConfigurationVersion,
            facts.ProviderCode,
            facts.ProviderServiceCode,
            facts.QuoteInputIdentity,
            facts.OriginLocationCode,
            facts.DestinationLocationCode,
            facts.WeightGrams,
            facts.DeclaredValue,
            facts.LengthCentimeters,
            facts.WidthCentimeters,
            facts.HeightCentimeters,
            facts.SellerOrderStatus,
            facts.IsCancelled,
            facts.PaymentLocked,
            snapshot.CanCreate,
            snapshot.PrimaryReason,
            facts.OrderLineIds,
            snapshot.CanCreate,
            snapshot.CanTrack,
            snapshot.CanCancel,
            snapshot.CanLabel,
            snapshot.BlockReasonCodes,
            snapshot.ConfiguredSender,
            snapshot.PackageReady,
            snapshot.AllowPackageCompletion,
            snapshot.HasEditableSenderPostal,
            snapshot.HasEditableSenderContact,
            snapshot.LatestCreateOperation);

    private static bool HasFullPackageFromFacts(OrderCommittedCarrierShipmentFacts facts)
        => facts.WeightGrams is > 0m
           && facts.LengthCentimeters is > 0m
           && facts.WidthCentimeters is > 0m
           && facts.HeightCentimeters is > 0m
           && !string.IsNullOrWhiteSpace(facts.OriginLocationCode)
           && !string.IsNullOrWhiteSpace(facts.DestinationLocationCode);

    private static ShippingCarrierConfiguredSenderView BuildConfiguredSender(
        ShippingCarrierAccountExecutionContext? context,
        OrderCommittedCarrierShipmentFacts facts,
        ShippingCarrierShipmentSenderCompletion? completion)
    {
        if (completion is not null)
        {
            return new ShippingCarrierConfiguredSenderView(
                completion.FullName,
                completion.Mobile,
                completion.AddressLine,
                completion.PostalCode,
                completion.LocationCode);
        }

        var binding = context?.MethodBinding;
        return new ShippingCarrierConfiguredSenderView(
            context?.DisplayName,
            null,
            binding?.OriginAddressLine,
            null,
            binding?.OriginExternalCityCode ?? facts.OriginLocationCode);
    }

    private static bool IsSenderComplete(ShippingCarrierConfiguredSenderView sender)
        => !string.IsNullOrWhiteSpace(sender.FullName)
           && !string.IsNullOrWhiteSpace(sender.Mobile)
           && !string.IsNullOrWhiteSpace(sender.AddressLine)
           && !string.IsNullOrWhiteSpace(sender.LocationCode);

    private static ShippingCarrierShipmentPartyInput? ResolveSenderForCreate(
        ReadinessSnapshot snapshot,
        OrderCommittedCarrierShipmentFacts facts,
        ShippingCarrierShipmentPartyInput requested)
    {
        var configured = snapshot.ConfiguredSender;
        var fullName = FirstNonEmpty(requested.FullName, configured.FullName);
        var mobile = FirstNonEmpty(requested.Mobile, configured.Mobile);
        var address = FirstNonEmpty(requested.AddressLine, configured.AddressLine);
        var postal = FirstNonEmpty(requested.PostalCode, configured.PostalCode);
        var location = FirstNonEmpty(requested.LocationCode, configured.LocationCode, facts.OriginLocationCode);
        if (string.IsNullOrWhiteSpace(fullName)
            || string.IsNullOrWhiteSpace(mobile)
            || string.IsNullOrWhiteSpace(address)
            || string.IsNullOrWhiteSpace(location))
        {
            return null;
        }

        if (!string.Equals(location.Trim(), facts.OriginLocationCode, StringComparison.Ordinal))
        {
            return null;
        }

        return new ShippingCarrierShipmentPartyInput(fullName, mobile, address, location, postal);
    }

    private static string? FirstNonEmpty(params string?[] values)
    {
        foreach (var value in values)
        {
            if (!string.IsNullOrWhiteSpace(value))
            {
                return value.Trim();
            }
        }

        return null;
    }

    private async Task<ShippingCarrierShipmentSenderCompletion?> LoadSenderCompletionAsync(
        ShippingDraftOwner owner,
        OrderCommittedCarrierShipmentFacts facts,
        CancellationToken cancellationToken)
        => await db.CarrierShipmentSenderCompletions.AsNoTracking()
            .SingleOrDefaultAsync(
                x => x.CheckoutId == facts.CheckoutId
                     && x.ShippingGroupId == facts.ShippingGroupId
                     && x.OwnerKind == owner.Kind
                     && x.OwnerTenantId == owner.TenantId
                     && x.OwnerSellerPartyId == owner.SellerPartyId,
                cancellationToken);

    private async Task<ShippingCarrierShipmentPackageCompletion?> LoadPackageCompletionAsync(
        ShippingDraftOwner owner,
        OrderCommittedCarrierShipmentFacts facts,
        CancellationToken cancellationToken)
        => await db.CarrierShipmentPackageCompletions.AsNoTracking()
            .SingleOrDefaultAsync(
                x => x.CheckoutId == facts.CheckoutId
                     && x.ShippingGroupId == facts.ShippingGroupId
                     && x.OwnerKind == owner.Kind
                     && x.OwnerTenantId == owner.TenantId
                     && x.OwnerSellerPartyId == owner.SellerPartyId,
                cancellationToken);

    private static string CreateOperationKey(Guid checkoutId, Guid shippingGroupId)
        => $"create:{checkoutId:N}:{shippingGroupId:N}";

    private static ShippingDraftOwner ToDomainOwner(ShippingAuthoringOwnerScope owner)
        => owner.Kind switch
        {
            ShippingAuthoringOwnerKind.Store when owner.TenantId is { } tenant =>
                ShippingDraftOwner.ForStore(tenant),
            ShippingAuthoringOwnerKind.Marketplace when owner.TenantId is null && owner.SellerPartyId is null =>
                ShippingDraftOwner.ForMarketplace(),
            ShippingAuthoringOwnerKind.Seller when owner.TenantId is null && owner.SellerPartyId is { } seller =>
                ShippingDraftOwner.ForSeller(seller),
            _ => throw new InvalidOperationException(ShippingErrorCodes.CarrierShipmentRequestInvalid),
        };

    private sealed record ReadinessSnapshot(
        bool CanCreate,
        bool CanTrack,
        bool CanCancel,
        bool CanLabel,
        IReadOnlyList<string> BlockReasonCodes,
        string? PrimaryReason,
        ShippingCarrierConfiguredSenderView ConfiguredSender,
        bool PackageReady,
        bool AllowPackageCompletion,
        bool HasEditableSenderPostal,
        bool HasEditableSenderContact,
        ShippingCarrierLatestCreateOperationView? LatestCreateOperation,
        ShippingCarrierShipmentPackageInput? EffectivePackage);
}
