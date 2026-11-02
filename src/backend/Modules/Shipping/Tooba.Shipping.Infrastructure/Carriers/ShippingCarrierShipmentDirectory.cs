using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Npgsql;
using Tooba.BuildingBlocks;
using Tooba.BuildingBlocks.Results;
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

internal sealed class ShippingCarrierShipmentDirectory(
    ShippingDbContext db,
    IShippingCarrierAccountDirectory accounts,
    ICarrierShipmentProviderRegistry providers,
    IOrderCommittedCarrierShipmentFactsReader orderFacts,
    IOptionsMonitor<TipaxV4Options> tipaxOptions) : IShippingCarrierShipmentOperations
{
    public async Task<Result<ShippingCarrierShipmentOperationView>> CreateAsync(
        ShippingAuthoringOwnerScope owner,
        ShippingCarrierShipmentCreateRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(owner);
        ArgumentNullException.ThrowIfNull(request);
        if (request.CheckoutId == Guid.Empty || request.ShippingGroupId == Guid.Empty)
        {
            return Fail(ShippingErrorCodes.CarrierShipmentNotEligible);
        }

        var facts = await orderFacts.GetAsync(request.CheckoutId, request.ShippingGroupId, cancellationToken);
        if (facts is null || !facts.CanExecuteCarrierShipment)
        {
            return Fail(ShippingErrorCodes.CarrierShipmentNotEligible);
        }

        if (!ShippingCarrierOwnerScopeMatching.Matches(owner, facts))
        {
            return Fail(ShippingErrorCodes.CarrierShipmentNotEligible);
        }

        if (string.IsNullOrWhiteSpace(facts.OriginLocationCode)
            || string.IsNullOrWhiteSpace(facts.DestinationLocationCode)
            || facts.WeightGrams is not > 0m
            || facts.LengthCentimeters is not > 0m
            || facts.WidthCentimeters is not > 0m
            || facts.HeightCentimeters is not > 0m
            || facts.CarrierAccountId is not { } accountId
            || facts.ConfigurationVersion is not { } configurationVersion
            || string.IsNullOrWhiteSpace(facts.ProviderServiceCode))
        {
            return Fail(ShippingErrorCodes.CarrierShipmentNotEligible);
        }

        var senderLocation = (request.Sender.LocationCode ?? string.Empty).Trim();
        if (!string.Equals(senderLocation, facts.OriginLocationCode, StringComparison.Ordinal))
        {
            return Fail(ShippingErrorCodes.CarrierShipmentRequestInvalid);
        }

        var recipient = new ShippingCarrierShipmentPartyInput(
            facts.RecipientName,
            facts.RecipientMobile,
            facts.RecipientAddress,
            facts.DestinationLocationCode,
            facts.PostalCode);
        var package = new ShippingCarrierShipmentPackageInput(
            facts.WeightGrams.Value,
            facts.DeclaredValue ?? 0m,
            facts.LengthCentimeters.Value,
            facts.WidthCentimeters.Value,
            facts.HeightCentimeters.Value,
            facts.CarrierCollectAmount);
        var serviceId = facts.ProviderServiceCode;

        var domainOwner = ToDomainOwner(owner);
        var context = await accounts.TryResolveExecutionContextAsync(
            domainOwner.Kind,
            domainOwner.TenantId,
            domainOwner.SellerPartyId,
            accountId,
            cancellationToken);
        if (context is null || !context.IsActive || string.IsNullOrWhiteSpace(context.Username))
        {
            return Fail(ShippingErrorCodes.CarrierUnconfigured);
        }

        if (context.ConfigurationVersion != configurationVersion)
        {
            return Fail(ShippingErrorCodes.CarrierShipmentNotEligible);
        }

        if (!providers.TryResolve(context.ProviderCode, out var provider) || provider is null)
        {
            return Fail(ShippingErrorCodes.CarrierProviderUnknown);
        }

        if (!provider.Capabilities.CanCreate)
        {
            return Fail(ShippingErrorCodes.CarrierShipmentUnsupported);
        }

        var options = tipaxOptions.CurrentValue;
        if (!Uri.TryCreate(options.SuccessCallbackUrl, UriKind.Absolute, out _)
            || !Uri.TryCreate(options.FailureCallbackUrl, UriKind.Absolute, out _))
        {
            return Fail(ShippingErrorCodes.CarrierShipmentNotEligible);
        }

        var operationKey = $"create:{request.CheckoutId:N}:{request.ShippingGroupId:N}";
        var existing = await db.CarrierShipmentOperations
            .AsNoTracking()
            .SingleOrDefaultAsync(x => x.OperationKey == operationKey, cancellationToken);
        if (existing is not null)
        {
            return Result.Success(Map(existing, provider.Capabilities));
        }

        var now = DateTimeOffset.UtcNow;
        var pending = ShippingCarrierShipmentOperation.Begin(
            UuidV7.New(),
            operationKey,
            ShippingCarrierShipmentKind.Create,
            domainOwner.Kind,
            domainOwner.TenantId,
            domainOwner.SellerPartyId,
            request.CheckoutId,
            request.ShippingGroupId,
            context.ProviderCode,
            context.AccountId,
            context.ConfigurationVersion,
            now);
        db.CarrierShipmentOperations.Add(pending);
        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException exception) when (IsUnique(exception))
        {
            db.Entry(pending).State = EntityState.Detached;
            var winner = await db.CarrierShipmentOperations
                .AsNoTracking()
                .SingleAsync(x => x.OperationKey == operationKey, cancellationToken);
            return Result.Success(Map(winner, provider.Capabilities));
        }

        var tracked = await db.CarrierShipmentOperations
            .SingleAsync(x => x.OperationId == pending.OperationId, cancellationToken);
        var outcome = await provider.CreateAsync(
            new CarrierShipmentCreateRequest(
                operationKey,
                context.AccountId,
                context.ConfigurationVersion,
                new CarrierAccountExecution(
                    context.AccountId,
                    context.ConfigurationVersion,
                    context.Username,
                    context.Password,
                    context.CurrencyCode,
                    context.MethodBinding?.PaymentTypeId ?? "0",
                    context.MethodBinding?.PickupTypeId ?? "0",
                    context.MethodBinding?.DistributionTypeId ?? "0",
                    context.MethodBinding?.PackingId ?? "0",
                    context.MethodBinding?.PackageContentId ?? "0",
                    context.MethodBinding?.PackTypeId ?? "0",
                    context.ApiBaseUrl,
                    context.TokenUrl),
                serviceId,
                new CarrierShipmentParty(
                    request.Sender.FullName,
                    request.Sender.Mobile,
                    request.Sender.AddressLine,
                    facts.OriginLocationCode,
                    request.Sender.PostalCode),
                new CarrierShipmentParty(
                    recipient.FullName,
                    recipient.Mobile,
                    recipient.AddressLine,
                    recipient.LocationCode,
                    recipient.PostalCode),
                new CarrierShipmentPackage(
                    package.WeightGrams,
                    package.DeclaredValue,
                    package.LengthCentimeters,
                    package.WidthCentimeters,
                    package.HeightCentimeters,
                    package.CollectAmount),
                options.SuccessCallbackUrl,
                options.FailureCallbackUrl),
            cancellationToken);

        ApplyCreateOutcome(tracked, outcome, DateTimeOffset.UtcNow);
        await db.SaveChangesAsync(cancellationToken);
        return Result.Success(Map(tracked, provider.Capabilities));
    }

    public async Task<Result<IReadOnlyList<ShippingCarrierShipmentOperationView>>> ListAsync(
        ShippingAuthoringOwnerScope owner,
        Guid checkoutId,
        CancellationToken cancellationToken)
    {
        if (checkoutId == Guid.Empty)
        {
            return Result.Failure<IReadOnlyList<ShippingCarrierShipmentOperationView>>(
                new SemanticError(ShippingErrorCodes.CarrierShipmentNotEligible));
        }

        var domainOwner = ToDomainOwner(owner);
        var rows = await db.CarrierShipmentOperations
            .AsNoTracking()
            .Where(x => x.CheckoutId == checkoutId
                        && x.OwnerKind == domainOwner.Kind
                        && x.OwnerTenantId == domainOwner.TenantId
                        && x.OwnerSellerPartyId == domainOwner.SellerPartyId)
            .OrderBy(x => x.AttemptedAt)
            .ToListAsync(cancellationToken);
        return Result.Success<IReadOnlyList<ShippingCarrierShipmentOperationView>>(
            rows.Select(row => Map(row, ResolveCapabilities(row.ProviderCode))).ToArray());
    }

    public async Task<Result<ShippingCarrierShipmentOperationView>> GetAsync(
        ShippingAuthoringOwnerScope owner,
        Guid operationId,
        CancellationToken cancellationToken)
    {
        var row = await LoadOwnedAsync(owner, operationId, cancellationToken);
        if (row is null)
        {
            return Fail(ShippingErrorCodes.CarrierShipmentNotFound);
        }

        var capabilities = ResolveCapabilities(row.ProviderCode);
        return Result.Success(Map(row, capabilities));
    }

    public async Task<Result<ShippingCarrierShipmentOperationView>> TrackAsync(
        ShippingAuthoringOwnerScope owner,
        Guid operationId,
        CancellationToken cancellationToken)
    {
        var row = await LoadOwnedAsync(owner, operationId, cancellationToken, track: true);
        if (row is null)
        {
            return Fail(ShippingErrorCodes.CarrierShipmentNotFound);
        }

        if (!providers.TryResolve(row.ProviderCode, out var provider) || provider is null || !provider.Capabilities.CanTrack)
        {
            return Fail(ShippingErrorCodes.CarrierShipmentUnsupported);
        }

        var domainOwner = ToDomainOwner(owner);
        var context = await accounts.TryResolveExecutionContextAsync(
            domainOwner.Kind,
            domainOwner.TenantId,
            domainOwner.SellerPartyId,
            row.AccountId,
            cancellationToken);
        if (context is null || string.IsNullOrWhiteSpace(context.Username))
        {
            return Fail(ShippingErrorCodes.CarrierUnconfigured);
        }

        var outcome = await provider.TrackAsync(
            new CarrierShipmentTrackRequest(
                row.OperationKey,
                new CarrierAccountExecution(
                    context.AccountId,
                    context.ConfigurationVersion,
                    context.Username,
                    context.Password,
                    context.CurrencyCode,
                    "0",
                    "0",
                    "0",
                    "0",
                    "0",
                    "0",
                    context.ApiBaseUrl,
                    context.TokenUrl),
                row.ProviderTrackingCode,
                row.OperationKey),
            cancellationToken);
        if (outcome.Outcome == CarrierShipmentOutcome.Succeeded)
        {
            row.ApplyTrack(DateTimeOffset.UtcNow, outcome.TrackingCode, outcome.ProviderStatus);
            await db.SaveChangesAsync(cancellationToken);
        }

        return Result.Success(Map(row, provider.Capabilities));
    }

    public async Task<Result<ShippingCarrierShipmentOperationView>> CancelAsync(
        ShippingAuthoringOwnerScope owner,
        Guid operationId,
        CancellationToken cancellationToken)
    {
        var row = await LoadOwnedAsync(owner, operationId, cancellationToken, track: true);
        if (row is null)
        {
            return Fail(ShippingErrorCodes.CarrierShipmentNotFound);
        }

        if (!providers.TryResolve(row.ProviderCode, out var provider) || provider is null || !provider.Capabilities.CanCancel)
        {
            return Fail(ShippingErrorCodes.CarrierShipmentUnsupported);
        }

        if (string.IsNullOrWhiteSpace(row.ProviderTrackingCode))
        {
            return Fail(ShippingErrorCodes.CarrierShipmentNotEligible);
        }

        var domainOwner = ToDomainOwner(owner);
        var context = await accounts.TryResolveExecutionContextAsync(
            domainOwner.Kind,
            domainOwner.TenantId,
            domainOwner.SellerPartyId,
            row.AccountId,
            cancellationToken);
        if (context is null || string.IsNullOrWhiteSpace(context.Username))
        {
            return Fail(ShippingErrorCodes.CarrierUnconfigured);
        }

        var outcome = await provider.CancelAsync(
            new CarrierShipmentCancelRequest(
                row.OperationKey,
                new CarrierAccountExecution(
                    context.AccountId,
                    context.ConfigurationVersion,
                    context.Username,
                    context.Password,
                    context.CurrencyCode,
                    "0",
                    "0",
                    "0",
                    "0",
                    "0",
                    "0",
                    context.ApiBaseUrl,
                    context.TokenUrl),
                row.ProviderTrackingCode),
            cancellationToken);
        if (outcome.Outcome == CarrierShipmentOutcome.Succeeded)
        {
            row.ApplyCancel(DateTimeOffset.UtcNow, outcome.ProviderStatus);
            await db.SaveChangesAsync(cancellationToken);
        }
        else if (outcome.Outcome == CarrierShipmentOutcome.Unknown)
        {
            return Fail(ShippingErrorCodes.CarrierShipmentUnknown);
        }

        return Result.Success(Map(row, provider.Capabilities));
    }

    public Task<Result<ShippingCarrierShipmentOperationView>> LabelAsync(
        ShippingAuthoringOwnerScope owner,
        Guid operationId,
        CancellationToken cancellationToken)
        => Task.FromResult(Fail(ShippingErrorCodes.CarrierShipmentUnsupported));

    private async Task<ShippingCarrierShipmentOperation?> LoadOwnedAsync(
        ShippingAuthoringOwnerScope owner,
        Guid operationId,
        CancellationToken cancellationToken,
        bool track = false)
    {
        var query = track
            ? db.CarrierShipmentOperations.AsQueryable()
            : db.CarrierShipmentOperations.AsNoTracking();
        var row = await query.SingleOrDefaultAsync(x => x.OperationId == operationId, cancellationToken);
        if (row is null)
        {
            return null;
        }

        var domainOwner = ToDomainOwner(owner);
        if (row.OwnerKind != domainOwner.Kind
            || !string.Equals(row.OwnerTenantId, domainOwner.TenantId, StringComparison.Ordinal)
            || row.OwnerSellerPartyId != domainOwner.SellerPartyId)
        {
            return null;
        }

        return row;
    }

    private CarrierShipmentCapabilities ResolveCapabilities(string providerCode)
        => providers.TryResolve(providerCode, out var provider) && provider is not null
            ? provider.Capabilities
            : new CarrierShipmentCapabilities(providerCode, false, false, false, false);

    private static void ApplyCreateOutcome(
        ShippingCarrierShipmentOperation row,
        CarrierShipmentResult outcome,
        DateTimeOffset now)
    {
        switch (outcome.Outcome)
        {
            case CarrierShipmentOutcome.Succeeded:
                row.Succeed(now, outcome.TrackingCode, outcome.ProviderOrderId, outcome.ProviderStatus);
                break;
            case CarrierShipmentOutcome.Unknown:
                row.MarkUnknown(now, outcome.ReasonCode ?? ShippingErrorCodes.CarrierShipmentUnknown);
                break;
            default:
                row.Fail(now, outcome.ReasonCode ?? ShippingErrorCodes.CarrierShipmentRequestInvalid);
                break;
        }
    }

    private static ShippingCarrierShipmentOperationView Map(
        ShippingCarrierShipmentOperation row,
        CarrierShipmentCapabilities capabilities)
        => new(
            row.OperationId,
            row.OperationKey,
            (ShippingCarrierShipmentKindView)row.Kind,
            (ShippingCarrierShipmentStateView)row.State,
            row.CheckoutId,
            row.ShippingGroupId,
            row.ProviderCode,
            row.AccountId,
            row.ConfigurationVersion,
            row.ProviderTrackingCode,
            row.ProviderOrderId,
            row.ProviderStatus,
            row.LastErrorCode,
            capabilities.CanCreate,
            capabilities.CanTrack,
            capabilities.CanCancel,
            capabilities.CanLabel,
            row.AttemptedAt,
            row.CompletedAt);

    private static Result<ShippingCarrierShipmentOperationView> Fail(string code)
        => Result.Failure<ShippingCarrierShipmentOperationView>(new SemanticError(code));

    private static bool IsUnique(DbUpdateException exception)
        => exception.InnerException is PostgresException { SqlState: "23505" };

    private static ShippingDraftOwner ToDomainOwner(ShippingAuthoringOwnerScope owner)
        => owner.Kind switch
        {
            ShippingAuthoringOwnerKind.Store when owner.TenantId is { } tenant =>
                ShippingDraftOwner.ForStore(tenant),
            ShippingAuthoringOwnerKind.Marketplace when owner.TenantId is null && owner.SellerPartyId is null =>
                ShippingDraftOwner.ForMarketplace(),
            ShippingAuthoringOwnerKind.Seller when owner.TenantId is null && owner.SellerPartyId is { } seller =>
                ShippingDraftOwner.ForSeller(seller),
            _ => throw new ContractOperationException(ShippingErrorCodes.CarrierShipmentRequestInvalid),
        };
}
