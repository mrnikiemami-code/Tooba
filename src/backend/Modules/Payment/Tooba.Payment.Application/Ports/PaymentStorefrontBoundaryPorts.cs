namespace Tooba.Payment.Application.Ports;

public interface ICheckoutActorPolicyPort
{
    Task EnsureCheckoutActorAsync(CancellationToken cancellationToken);
}

/// <summary>Checkout ownership/payable read seam for Payment storefront (Host adapter; no Checkout redesign).</summary>
public sealed record StorefrontCheckoutPaymentAccessDto(
    Guid CheckoutId,
    decimal PayableAmount,
    string Currency,
    string? OrderNumber);

/// <summary>Narrow checkout access for Payment without Host composer dependency from Application.</summary>
[Obsolete("Use Order.Contracts ICheckoutPaymentAccessReader and ICheckoutActorPolicyPort.")]
public interface IStorefrontCheckoutPaymentAccessPort : ICheckoutActorPolicyPort
{
    /// <summary>Storefront checkout actor gate (auth/guest policy).</summary>
    Task EnsureCheckoutActorAsync(CancellationToken cancellationToken);

    /// <summary>Owned checkout for wallet quote / initiate.</summary>
    Task<StorefrontCheckoutPaymentAccessDto?> GetForMutationAsync(
        Guid checkoutId,
        Guid cartId,
        string? guestSecret,
        CancellationToken cancellationToken);

    /// <summary>Owned checkout for payment result (cart may be finalized).</summary>
    Task<StorefrontCheckoutPaymentAccessDto?> GetOwnedForPaymentResultAsync(
        Guid checkoutId,
        string? guestSecret,
        CancellationToken cancellationToken);

    /// <summary>Owned checkout with cart context (sandbox order number).</summary>
    Task<StorefrontCheckoutPaymentAccessDto?> GetOwnedAsync(
        Guid checkoutId,
        Guid cartId,
        string? guestSecret,
        CancellationToken cancellationToken);
}

/// <summary>Media proof upload for manual payment (no Media.Application from Payment.Application).</summary>
[Obsolete("Use Media.Contracts IMediaAssetUploadPort.")]
public interface IPaymentProofMediaPort
{
    Task<Guid> UploadAsync(
        Stream stream,
        string fileName,
        string contentType,
        Guid actorUserId,
        CancellationToken cancellationToken);
}

/// <summary>Unpaid-retry supply ensure (Host/Inventory via Contracts/adapter).</summary>
[Obsolete("Use Order.Contracts IOrderUnpaidRetrySupplyPort.")]
public interface IPaymentUnpaidRetrySupplyPort
{
    Task EnsureRetrySupplyAsync(Guid checkoutId, CancellationToken cancellationToken);
}

/// <summary>Gateway catalog / sandbox / manual policy (Infrastructure-backed).</summary>
public interface IPaymentGatewayCatalogPort
{
    string DefaultProvider { get; }
    bool ManualCardToCardEnabled { get; }
    string ManualProofRequirement { get; }
    string ManualPaymentInstructions { get; }
    string StoreDisplayName { get; }
    int ManualPaymentReviewHoldHours { get; }
    bool IsOnlineGatewayOffered();
    bool IsSandboxSimulatorEnabled();
    void MarkSandboxDecline(string providerRequestReference);
}

/// <summary>Webhook signature verification without leaking Infrastructure options into Endpoints.</summary>
public interface IPaymentWebhookSignatureVerifier
{
    string SignatureHeaderName { get; }
    bool TryValidate(ReadOnlySpan<byte> body, string? signatureHeader, out string errorCode);
}

/// <summary>Order enrichment for admin payment grid (Contracts/Host adapter; no OrderDbContext in Payment).</summary>
[Obsolete("Use Order.Contracts IPaymentAdminOrderEnrichmentReader.")]
public interface IPaymentAdminOrderEnrichmentPort
{
    Task<IReadOnlyList<Guid>> ResolveSearchCheckoutIdsAsync(string search, CancellationToken cancellationToken);

    Task<IReadOnlyList<Guid>> ResolveSupplyFilterCheckoutIdsAsync(
        IReadOnlyList<string> wantedValues,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<Guid>> ResolveReservationFilterCheckoutIdsAsync(
        IReadOnlyList<string> wantedValues,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<AdminPaymentOrderEnrichmentDto>> EnrichAsync(
        IReadOnlyList<Guid> checkoutIds,
        CancellationToken cancellationToken);
}

/// <summary>Order display fields for admin payment grid rows.</summary>
public sealed record AdminPaymentOrderEnrichmentDto(
    Guid CheckoutId,
    string OrderReference,
    string CustomerDisplayName,
    string SupplyStatus,
    string ReservationLabel,
    string ReservationLabelEn,
    string ReservationState,
    int? ReservationCycleNumber,
    bool ReservationRetryPossible,
    bool ReservationNeedsReacquire,
    bool ReservationRetryLimitReached);
