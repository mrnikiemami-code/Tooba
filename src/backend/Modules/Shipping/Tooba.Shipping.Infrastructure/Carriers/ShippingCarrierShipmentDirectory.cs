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
    IShippingCarrierExecutionReadiness readiness,
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
        if (facts is null)
        {
            return Fail(ShippingErrorCodes.CarrierShipmentNotEligible);
        }

        var operationKey = $"create:{request.CheckoutId:N}:{request.ShippingGroupId:N}";
        var existing = await db.CarrierShipmentOperations
            .AsNoTracking()
            .SingleOrDefaultAsync(x => x.OperationKey == operationKey, cancellationToken);
        if (existing is not null)
        {
            return Result.Success(Map(existing, ResolveCapabilities(existing.ProviderCode)));
        }

        var (allowed, failureCode, resolvedSender, resolvedPackage) = await readiness.EvaluateCreateAsync(
            owner,
            facts,
            request.Sender,
            cancellationToken);
        if (!allowed || resolvedSender is null || resolvedPackage is null)
        {
            return Fail(failureCode ?? ShippingErrorCodes.CarrierShipmentNotEligible);
        }

        if (facts.CarrierAccountId is not { } accountId
            || facts.ConfigurationVersion is not { } configurationVersion
            || string.IsNullOrWhiteSpace(facts.ProviderServiceCode))
        {
            return Fail(ShippingErrorCodes.CarrierShipmentNotEligible);
        }

        var recipient = new ShippingCarrierShipmentPartyInput(
            facts.RecipientName,
            facts.RecipientMobile,
            facts.RecipientAddress,
            facts.DestinationLocationCode!,
            facts.PostalCode);
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

        if (!providers.TryResolve(context.ProviderCode, out var provider) || provider is null)
        {
            return Fail(ShippingErrorCodes.CarrierProviderUnknown);
        }

        if (!provider.Capabilities.CanCreate)
        {
            return Fail(ShippingErrorCodes.CarrierShipmentUnsupported);
        }

        var options = tipaxOptions.CurrentValue;

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
                    resolvedSender.FullName,
                    resolvedSender.Mobile,
                    resolvedSender.AddressLine,
                    facts.OriginLocationCode!,
                    resolvedSender.PostalCode),
                new CarrierShipmentParty(
                    recipient.FullName,
                    recipient.Mobile,
                    recipient.AddressLine,
                    recipient.LocationCode,
                    recipient.PostalCode),
                new CarrierShipmentPackage(
                    resolvedPackage.WeightGrams,
                    resolvedPackage.DeclaredValue,
                    resolvedPackage.LengthCentimeters,
                    resolvedPackage.WidthCentimeters,
                    resolvedPackage.HeightCentimeters,
                    resolvedPackage.CollectAmount),
                options.SuccessCallbackUrl,
                options.FailureCallbackUrl),
            cancellationToken);

        ApplyCreateOutcome(tracked, outcome, DateTimeOffset.UtcNow);
        await UpsertSenderCompletionAsync(domainOwner, facts, resolvedSender, DateTimeOffset.UtcNow, cancellationToken);
        await db.SaveChangesAsync(cancellationToken);
        return Result.Success(Map(tracked, provider.Capabilities));
    }

    public async Task<Result<ShippingGroupCarrierExecutionCapabilityView>> SavePackageCompletionAsync(
        ShippingAuthoringOwnerScope owner,
        ShippingCarrierShipmentPackageCompletionRequest request,
        CancellationToken cancellationToken)
    {
        if (request.CheckoutId == Guid.Empty || request.ShippingGroupId == Guid.Empty)
        {
            return Result.Failure<ShippingGroupCarrierExecutionCapabilityView>(
                new SemanticError(ShippingErrorCodes.CarrierShipmentRequestInvalid));
        }

        var facts = await orderFacts.GetAsync(request.CheckoutId, request.ShippingGroupId, cancellationToken);
        if (facts is null || !ShippingCarrierOwnerScopeMatching.Matches(owner, facts))
        {
            return Result.Failure<ShippingGroupCarrierExecutionCapabilityView>(
                new SemanticError(ShippingErrorCodes.CarrierShipmentNotEligible));
        }

        if (!facts.HasCarrierQuote || string.IsNullOrWhiteSpace(facts.OriginLocationCode))
        {
            return Result.Failure<ShippingGroupCarrierExecutionCapabilityView>(
                new SemanticError(ShippingErrorCodes.CarrierShipmentNotEligible));
        }

        var domainOwner = ToDomainOwner(owner);
        var existing = await db.CarrierShipmentPackageCompletions
            .SingleOrDefaultAsync(
                x => x.CheckoutId == request.CheckoutId
                     && x.ShippingGroupId == request.ShippingGroupId
                     && x.OwnerKind == domainOwner.Kind
                     && x.OwnerTenantId == domainOwner.TenantId
                     && x.OwnerSellerPartyId == domainOwner.SellerPartyId,
                cancellationToken);
        var now = DateTimeOffset.UtcNow;
        if (existing is null)
        {
            db.CarrierShipmentPackageCompletions.Add(
                ShippingCarrierShipmentPackageCompletion.Create(
                    Guid.NewGuid(),
                    domainOwner.Kind,
                    domainOwner.TenantId,
                    domainOwner.SellerPartyId,
                    request.CheckoutId,
                    request.ShippingGroupId,
                    request.WeightGrams,
                    request.DeclaredValue,
                    request.LengthCentimeters,
                    request.WidthCentimeters,
                    request.HeightCentimeters,
                    now));
        }
        else
        {
            db.CarrierShipmentPackageCompletions.Remove(existing);
            db.CarrierShipmentPackageCompletions.Add(
                ShippingCarrierShipmentPackageCompletion.Create(
                    existing.CompletionId,
                    domainOwner.Kind,
                    domainOwner.TenantId,
                    domainOwner.SellerPartyId,
                    request.CheckoutId,
                    request.ShippingGroupId,
                    request.WeightGrams,
                    request.DeclaredValue,
                    request.LengthCentimeters,
                    request.WidthCentimeters,
                    request.HeightCentimeters,
                    now));
        }

        await db.SaveChangesAsync(cancellationToken);
        return Result.Success(await readiness.BuildCapabilityAsync(owner, facts, cancellationToken));
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
    {
        var canTrack = capabilities.CanTrack
                       && row.State is ShippingCarrierShipmentState.Succeeded or ShippingCarrierShipmentState.Unknown;
        var canCancel = capabilities.CanCancel
                        && row.State == ShippingCarrierShipmentState.Succeeded
                        && !string.IsNullOrWhiteSpace(row.ProviderTrackingCode);
        return new(
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
            false,
            canTrack,
            canCancel,
            capabilities.CanLabel,
            row.AttemptedAt,
            row.CompletedAt);
    }

    private async Task UpsertSenderCompletionAsync(
        ShippingDraftOwner domainOwner,
        OrderCommittedCarrierShipmentFacts facts,
        ShippingCarrierShipmentPartyInput sender,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        var existing = await db.CarrierShipmentSenderCompletions
            .SingleOrDefaultAsync(
                x => x.CheckoutId == facts.CheckoutId
                     && x.ShippingGroupId == facts.ShippingGroupId
                     && x.OwnerKind == domainOwner.Kind
                     && x.OwnerTenantId == domainOwner.TenantId
                     && x.OwnerSellerPartyId == domainOwner.SellerPartyId,
                cancellationToken);
        if (existing is not null)
        {
            db.CarrierShipmentSenderCompletions.Remove(existing);
        }

        db.CarrierShipmentSenderCompletions.Add(
            ShippingCarrierShipmentSenderCompletion.Create(
                existing?.CompletionId ?? Guid.NewGuid(),
                domainOwner.Kind,
                domainOwner.TenantId,
                domainOwner.SellerPartyId,
                facts.CheckoutId,
                facts.ShippingGroupId,
                sender.FullName,
                sender.Mobile,
                sender.AddressLine,
                sender.PostalCode,
                sender.LocationCode,
                now));
    }

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
