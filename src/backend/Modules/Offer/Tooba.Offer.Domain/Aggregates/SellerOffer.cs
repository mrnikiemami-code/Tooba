using Tooba.Offer.Domain.Events;
using Tooba.BuildingBlocks;
using Tooba.BuildingBlocks.Results;
using Tooba.Offer.Domain.Errors;
using Tooba.Offer.Domain.ValueObjects;

namespace Tooba.Offer.Domain.Aggregates;

/// <summary>
/// Seller commercial listing for a Catalog variant; price and inventory are separate.
/// Expected invariant failures return <see cref="Result"/> (strategy A) — not SemanticException control flow.
/// </summary>
public sealed class SellerOffer : IHasDomainEvents
{
    private readonly DomainEventCollector _domainEvents = new();

    /// <summary>
    /// Stable offer identifier.
    /// </summary>
    public Guid OfferId { get; init; }

    /// <summary>
    /// Catalog variant identifier without a cross-module foreign key.
    /// </summary>
    public Guid CatalogVariantId { get; init; }

    /// <summary>
    /// Seller organization Party identifier.
    /// </summary>
    public Guid SellerPartyId { get; init; }

    /// <summary>
    /// Seller-owned SKU.
    /// </summary>
    public string? SellerSku { get; set; }

    /// <summary>
    /// Listing lifecycle status.
    /// </summary>
    public OfferStatus Status { get; set; }

    /// <summary>
    /// Sales channel.
    /// </summary>
    public SalesChannel Channel { get; init; }

    /// <summary>
    /// Creation timestamp.
    /// </summary>
    public DateTimeOffset CreatedAt { get; init; }

    /// <summary>
    /// Last update timestamp.
    /// </summary>
    public DateTimeOffset UpdatedAt { get; set; }

    /// <summary>
    /// Return policy choice.
    /// </summary>
    public string ReturnPolicyChoice { get; private set; } = "Default";

    /// <summary>
    /// Custom return window in days.
    /// </summary>
    public int? CustomReturnWindowDays { get; private set; }

    /// <summary>Optional minimum order quantity.</summary>
    public decimal? MinimumOrderQuantity { get; private set; }

    /// <summary>Optional maximum order quantity.</summary>
    public decimal? MaximumOrderQuantity { get; private set; }

    /// <summary>
    /// Explicit fulfillment responsibility of this listing, or <c>null</c> when it was never assigned.
    /// </summary>
    /// <remarks>
    /// <b>Never inferred.</b> A legacy/unassigned listing stays <c>null</c>; nothing in the storefront may read
    /// the seller id, the sales channel or a method name as if it were a responsibility.
    /// </remarks>
    public FulfillmentResponsibility? FulfillmentResponsibility { get; private set; }

    /// <summary>
    /// Explicit real Fulfillment shipping type identity of this listing, or <c>null</c> when unassigned.
    /// </summary>
    /// <remarks>
    /// Only the stable id is stored: no type code/label copy, no foreign key into another module's schema.
    /// </remarks>
    public Guid? FulfillmentShippingTypeId { get; private set; }

    /// <inheritdoc />
    public IReadOnlyCollection<IDomainEvent> DomainEvents => _domainEvents.Events;

    /// <summary>Updates the seller-owned SKU.</summary>
    public void UpdateSellerSku(string? sellerSku, DateTimeOffset now)
    {
        SellerSku = string.IsNullOrWhiteSpace(sellerSku) ? null : sellerSku.Trim();
        UpdatedAt = now;
    }

    /// <summary>
    /// Creates an offer without price or inventory.
    /// </summary>
    public static SellerOffer Create(
        Guid offerId,
        Guid catalogVariantId,
        Guid sellerPartyId,
        SalesChannel channel,
        string? sellerSku,
        DateTimeOffset now)
    {
        var offer = new SellerOffer
        {
            OfferId = offerId,
            CatalogVariantId = catalogVariantId,
            SellerPartyId = sellerPartyId,
            Channel = channel,
            SellerSku = string.IsNullOrWhiteSpace(sellerSku) ? null : sellerSku.Trim(),
            Status = OfferStatus.Draft,
            CreatedAt = now,
            UpdatedAt = now,
            ReturnPolicyChoice = "Default",
            CustomReturnWindowDays = null,
        };
        offer._domainEvents.Add(new OfferCreatedDomainEvent(offer));
        return offer;
    }

    /// <summary>
    /// Sets the listing return policy after Application validation.
    /// </summary>
    public void SetReturnPolicy(string choice, int? customReturnWindowDays, DateTimeOffset now)
    {
        var normalized = string.IsNullOrWhiteSpace(choice) ? "Default" : choice.Trim();
        ReturnPolicyChoice = normalized switch
        {
            "Custom" => "Custom",
            "NonReturnable" => "NonReturnable",
            _ => "Default",
        };
        CustomReturnWindowDays = ReturnPolicyChoice == "Custom" ? customReturnWindowDays : null;
        UpdatedAt = now;
    }

    /// <summary>Sets minimum and maximum order quantities.</summary>
    public Result SetOrderQuantityLimits(decimal? minimum, decimal? maximum, DateTimeOffset now)
    {
        if (minimum is { } min && min <= 0)
        {
            return Result.Failure(new SemanticError(OfferErrorCodes.MinQuantityInvalid));
        }

        if (maximum is { } max && max <= 0)
        {
            return Result.Failure(new SemanticError(OfferErrorCodes.MaxQuantityInvalid));
        }

        if (minimum is { } a && maximum is { } b && a > b)
        {
            return Result.Failure(new SemanticError(OfferErrorCodes.MinQuantityExceedsMax));
        }

        MinimumOrderQuantity = minimum;
        MaximumOrderQuantity = maximum;
        UpdatedAt = now;
        return Result.Success();
    }

    /// <summary>
    /// Assigns the explicit fulfillment responsibility (and the real shipping type of a Platform seller-free
    /// listing) to this offer, or clears the assignment.
    /// </summary>
    /// <remarks>
    /// This is the ONLY assignment point: the responsible Seller identity is always this aggregate's own
    /// <see cref="SellerPartyId"/>, and the Platform owner is resolved later from the trusted edition/tenant
    /// context — never from a caller-supplied owner id, account or display name.
    /// </remarks>
    /// <param name="responsibility">Explicit responsibility, or <c>null</c> to clear the assignment.</param>
    /// <param name="shippingTypeId">Real Fulfillment shipping type id, or <c>null</c> when unassigned.</param>
    /// <param name="now">Current server instant.</param>
    public Result AssignFulfillment(
        FulfillmentResponsibility? responsibility,
        Guid? shippingTypeId,
        DateTimeOffset now)
    {
        if (Status == OfferStatus.Archived)
        {
            return Result.Failure(new SemanticError(OfferErrorCodes.ArchivedCannotAssignFulfillment));
        }

        if (responsibility is null)
        {
            // Clearing is lawful and explicit; a stray type id alone would be a half assignment.
            if (shippingTypeId is not null)
            {
                return Result.Failure(new SemanticError(OfferErrorCodes.FulfillmentTypeWithoutResponsibility));
            }

            FulfillmentResponsibility = null;
            FulfillmentShippingTypeId = null;
            UpdatedAt = now;
            return Result.Success();
        }

        if (!Enum.IsDefined(responsibility.Value))
        {
            return Result.Failure(new SemanticError(OfferErrorCodes.FulfillmentResponsibilityUnsupported));
        }

        if (shippingTypeId is Guid typeId && typeId == Guid.Empty)
        {
            return Result.Failure(new SemanticError(OfferErrorCodes.FulfillmentTypeInvalid));
        }

        FulfillmentResponsibility = responsibility;
        FulfillmentShippingTypeId = shippingTypeId;
        UpdatedAt = now;
        return Result.Success();
    }

    /// <summary>
    /// Activates the listing without asserting price or stock validity.
    /// </summary>
    public Result Activate(DateTimeOffset now)
    {
        if (Status == OfferStatus.Archived)
        {
            return Result.Failure(new SemanticError(OfferErrorCodes.ArchivedCannotActivate));
        }

        Status = OfferStatus.Active;
        UpdatedAt = now;
        _domainEvents.Add(new OfferActivatedDomainEvent(this));
        return Result.Success();
    }

    /// <summary>
    /// Suspends the listing.
    /// </summary>
    public void Suspend(DateTimeOffset now)
    {
        Status = OfferStatus.Suspended;
        UpdatedAt = now;
        _domainEvents.Add(new OfferSuspendedDomainEvent(this));
    }

    /// <summary>
    /// Archives the listing and releases its seller, variant, and channel key.
    /// </summary>
    public void Archive(DateTimeOffset now)
    {
        Status = OfferStatus.Archived;
        UpdatedAt = now;
        _domainEvents.Add(new OfferArchivedDomainEvent(this));
    }

    /// <inheritdoc />
    public void ClearDomainEvents() => _domainEvents.Clear();
}
