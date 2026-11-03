using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Npgsql;
using Npgsql.EntityFrameworkCore.PostgreSQL;
using Tooba.BuildingBlocks;
using Tooba.BuildingBlocks.Results;
using Tooba.Fulfillment.Contracts.Operations;
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
    IFulfillmentAdminOperations fulfillment,
    IOptionsMonitor<TipaxV4Options> tipaxOptions) : IShippingCarrierShipmentOperations
{
    public async Task<Result<ShippingCarrierShipmentOperationView>> CreateAsync(
        ShippingAuthoringOwnerScope owner,
        Guid actorUserId,
        ShippingCarrierShipmentExecuteRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(owner);
        ArgumentNullException.ThrowIfNull(request);
        if (actorUserId == Guid.Empty
            || request.CheckoutId == Guid.Empty
            || request.ShippingGroupId == Guid.Empty)
        {
            return Fail(ShippingErrorCodes.CarrierShipmentNotEligible);
        }

        var facts = await orderFacts.GetAsync(request.CheckoutId, request.ShippingGroupId, cancellationToken);
        if (facts is null || !ShippingCarrierOwnerScopeMatching.Matches(owner, facts))
        {
            return Fail(ShippingErrorCodes.CarrierShipmentNotEligible);
        }

        var domainOwner = ToDomainOwner(owner);
        var requestedOccurrence = request.PackageDispatchId is { } locatorId && locatorId != Guid.Empty
            ? locatorId
            : (Guid?)null;

        if (requestedOccurrence is { } occurrenceLocator)
        {
            var byLocator = await FindCreateByPackageDispatchIdAsync(
                domainOwner, request.CheckoutId, request.ShippingGroupId, occurrenceLocator, cancellationToken);
            if (byLocator is not null)
            {
                return await ReplayOrResumeExistingAsync(owner, actorUserId, facts, byLocator, request, cancellationToken);
            }
        }

        var (allowed, failureCode, resolvedSender, resolvedPackageBase) = await readiness.EvaluateCreateAsync(
            owner,
            facts,
            requestedSender: null,
            cancellationToken);

        if (facts.CarrierAccountId is not { } accountId
            || facts.ConfigurationVersion is not { } configurationVersion
            || string.IsNullOrWhiteSpace(facts.ProviderServiceCode)
            || facts.FulfillmentId is not { } fulfillmentId
            || string.IsNullOrWhiteSpace(facts.DestinationLocationCode)
            || string.IsNullOrWhiteSpace(facts.OriginLocationCode))
        {
            return Fail(ShippingErrorCodes.CarrierShipmentNotEligible);
        }

        // After allocate/provider crash, remaining qty may be 0 while a Pending/Unknown create still
        // owns the durable intent — replay that operation instead of failing closed as exhausted.
        if (facts.ExecutableLines.Count == 0)
        {
            var openCreate = await FindOpenCreateForGroupAsync(
                domainOwner, request.CheckoutId, request.ShippingGroupId, cancellationToken);
            if (openCreate is not null)
            {
                return await ReplayOrResumeExistingAsync(owner, actorUserId, facts, openCreate, request, cancellationToken);
            }

            return Fail(ShippingErrorCodes.CarrierShipmentNotEligible);
        }

        if (!TryResolveLineSelection(facts, request.Lines, out var selectedLines, out var lineFailure))
        {
            return Fail(lineFailure ?? ShippingErrorCodes.CarrierShipmentNotEligible);
        }

        // Prefer request package; else readiness package; else accepted quote dims for intent replay.
        var packageBase = resolvedPackageBase
            ?? (facts.WeightGrams is > 0m
                && facts.LengthCentimeters is > 0m
                && facts.WidthCentimeters is > 0m
                && facts.HeightCentimeters is > 0m
                ? new ShippingCarrierShipmentPackageInput(
                    facts.WeightGrams.Value,
                    0m,
                    facts.LengthCentimeters.Value,
                    facts.WidthCentimeters.Value,
                    facts.HeightCentimeters.Value,
                    0m)
                : null);
        string? physicalFailure = null;
        ShippingCarrierShipmentPackageInput physicalPackage;
        if (packageBase is null
            || !TryResolveDispatchPhysicalPackage(
                request,
                selectedLines,
                facts,
                packageBase,
                out physicalPackage,
                out physicalFailure))
        {
            return Fail(physicalFailure ?? failureCode ?? ShippingErrorCodes.CarrierShipmentNotEligible);
        }

        var (priorDeclared, priorCollect) = await SumPriorPackageMoneyAsync(
            domainOwner,
            request.CheckoutId,
            request.ShippingGroupId,
            cancellationToken);
        var isFinalRemaining = IsFinalRemainingDispatch(facts, selectedLines);
        var allocatedPackage = ShippingCarrierPackageMoneyAllocator.Allocate(
            physicalPackage,
            selectedLines,
            facts,
            priorDeclared,
            priorCollect,
            isFinalRemaining);

        var physicalForFingerprint = allocatedPackage with { DeclaredValue = 0m, CollectAmount = 0m };
        var payloadFingerprint = ShippingCarrierDispatchIntent.ComputePayloadFingerprint(
            domainOwner,
            request.CheckoutId,
            request.ShippingGroupId,
            selectedLines,
            physicalForFingerprint);

        // No occurrence locator: converge only onto an open Pending/Unknown with the SAME payload
        // (crash/retry recovery). A Succeeded identical payload must mint a NEW occurrence.
        if (requestedOccurrence is null)
        {
            var openMatching = await FindOpenCreateMatchingPayloadAsync(
                domainOwner,
                request.CheckoutId,
                request.ShippingGroupId,
                payloadFingerprint,
                cancellationToken);
            if (openMatching is not null)
            {
                return await ReplayOrResumeExistingAsync(owner, actorUserId, facts, openMatching, request, cancellationToken);
            }
        }

        // New parcel occurrence: reuse client-held server id when provided and unknown; else mint UuidV7.
        var packageDispatchId = requestedOccurrence ?? ShippingCarrierDispatchIntent.MintOccurrenceId();
        var (operationKey, _) = ShippingCarrierDispatchIntent.Derive(
            packageDispatchId,
            domainOwner,
            request.CheckoutId,
            request.ShippingGroupId,
            selectedLines,
            physicalForFingerprint);

        var existing = await db.CarrierShipmentOperations
            .SingleOrDefaultAsync(
                x => x.OperationKey == operationKey
                     && x.OwnerKind == domainOwner.Kind
                     && x.OwnerTenantId == domainOwner.TenantId
                     && x.OwnerSellerPartyId == domainOwner.SellerPartyId,
                cancellationToken);
        if (existing is not null)
        {
            return await ReplayOrResumeExistingAsync(owner, actorUserId, facts, existing, request, cancellationToken);
        }

        if (!allowed || resolvedSender is null)
        {
            return Fail(failureCode ?? ShippingErrorCodes.CarrierShipmentNotEligible);
        }

        var recipient = new ShippingCarrierShipmentPartyInput(
            facts.RecipientName,
            facts.RecipientMobile,
            facts.RecipientAddress,
            facts.DestinationLocationCode,
            facts.PostalCode);

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

        if (context.AccountId != accountId
            || context.ConfigurationVersion != configurationVersion
            || !string.Equals(context.ProviderCode, facts.ProviderCode, StringComparison.OrdinalIgnoreCase))
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
        pending.SetExecutionInputSnapshot(SerializeExecutionSnapshot(
            new ExecutionSnapshot(
                packageDispatchId,
                fulfillmentId,
                FulfillmentShipmentId: null,
                selectedLines,
                resolvedSender,
                recipient,
                facts.OriginLocationCode!,
                facts.ProviderServiceCode!,
                allocatedPackage,
                context.AccountId,
                context.ConfigurationVersion,
                context.ProviderCode,
                facts.Currency,
                payloadFingerprint)));
        db.CarrierShipmentOperations.Add(pending);
        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException exception) when (IsUnique(exception))
        {
            db.Entry(pending).State = EntityState.Detached;
            var winner = await db.CarrierShipmentOperations
                .SingleAsync(x => x.OperationKey == operationKey, cancellationToken);
            if (!OwnerMatches(domainOwner, winner))
            {
                return Fail(ShippingErrorCodes.CarrierShipmentNotFound);
            }

            return await ReplayOrResumeExistingAsync(owner, actorUserId, facts, winner, request, cancellationToken);
        }

        var tracked = await db.CarrierShipmentOperations
            .SingleAsync(x => x.OperationId == pending.OperationId, cancellationToken);
        return await AllocateLinkAndDispatchAsync(
            owner,
            actorUserId,
            facts,
            tracked,
            context,
            provider,
            cancellationToken);
    }

    private async Task<Result<ShippingCarrierShipmentOperationView>> ReplayOrResumeExistingAsync(
        ShippingAuthoringOwnerScope owner,
        Guid actorUserId,
        OrderCommittedCarrierShipmentFacts facts,
        ShippingCarrierShipmentOperation existing,
        ShippingCarrierShipmentExecuteRequest request,
        CancellationToken cancellationToken)
    {
        if (TryDetectPayloadConflict(ToDomainOwner(owner), existing, request, facts, out var conflict))
        {
            return Fail(conflict!);
        }

        if (existing.State == ShippingCarrierShipmentState.Succeeded
            || existing.State == ShippingCarrierShipmentState.Failed)
        {
            var terminalContext = await ResolveAccountContextAsync(owner, existing.AccountId, cancellationToken);
            return Result.Success(Map(existing, ResolveCapabilities(existing.ProviderCode), terminalContext));
        }

        if (existing.State == ShippingCarrierShipmentState.Unknown
            || existing.ExternalDispatchAttempted)
        {
            if (existing.State == ShippingCarrierShipmentState.Pending && existing.ExternalDispatchAttempted)
            {
                var trackedUnknown = existing;
                if (db.Entry(trackedUnknown).State == EntityState.Detached)
                {
                    trackedUnknown = await db.CarrierShipmentOperations
                        .SingleAsync(x => x.OperationId == existing.OperationId, cancellationToken);
                }

                trackedUnknown.MarkUnknown(DateTimeOffset.UtcNow, ShippingErrorCodes.CarrierShipmentUnknown);
                await db.SaveChangesAsync(cancellationToken);
                existing = trackedUnknown;
            }

            var unknownContext = await ResolveAccountContextAsync(owner, existing.AccountId, cancellationToken);
            return Result.Success(Map(existing, ResolveCapabilities(existing.ProviderCode), unknownContext));
        }

        return await ResumePendingCreateAsync(owner, actorUserId, facts, existing, request, cancellationToken);
    }

    private async Task<Result<ShippingCarrierShipmentOperationView>> ResumePendingCreateAsync(
        ShippingAuthoringOwnerScope owner,
        Guid actorUserId,
        OrderCommittedCarrierShipmentFacts facts,
        ShippingCarrierShipmentOperation pending,
        ShippingCarrierShipmentExecuteRequest request,
        CancellationToken cancellationToken)
    {
        if (!TryReadExecutionSnapshot(pending.ExecutionInputSnapshotJson, out var snapshot) || snapshot is null)
        {
            return Fail(ShippingErrorCodes.CarrierShipmentNotEligible);
        }

        var domainOwner = ToDomainOwner(owner);
        if (TryDetectPayloadConflict(domainOwner, pending, request, facts, out var conflictCode))
        {
            return Fail(conflictCode!);
        }

        if (request.PackageDispatchId is { } locator
            && locator != Guid.Empty
            && locator != snapshot.PackageDispatchId)
        {
            return Fail(ShippingErrorCodes.CarrierShipmentIntentConflict);
        }

        var context = await accounts.TryResolveExecutionContextAsync(
            domainOwner.Kind,
            domainOwner.TenantId,
            domainOwner.SellerPartyId,
            pending.AccountId,
            cancellationToken);
        if (context is null || !context.IsActive || string.IsNullOrWhiteSpace(context.Username))
        {
            return Fail(ShippingErrorCodes.CarrierUnconfigured);
        }

        // Snapshot account/config/service is authoritative; refuse silent rotation/substitution.
        if (context.AccountId != snapshot.AccountId
            || context.ConfigurationVersion != snapshot.ConfigurationVersion
            || pending.AccountId != snapshot.AccountId
            || pending.ConfigurationVersion != snapshot.ConfigurationVersion
            || !string.Equals(context.ProviderCode, snapshot.ProviderCode, StringComparison.OrdinalIgnoreCase)
            || !string.Equals(pending.ProviderCode, snapshot.ProviderCode, StringComparison.OrdinalIgnoreCase))
        {
            return Fail(ShippingErrorCodes.CarrierShipmentNotEligible);
        }

        if (!providers.TryResolve(pending.ProviderCode, out var provider) || provider is null)
        {
            return Fail(ShippingErrorCodes.CarrierShipmentUnsupported);
        }

        var tracked = pending;
        if (db.Entry(tracked).State == EntityState.Detached)
        {
            tracked = await db.CarrierShipmentOperations
                .SingleAsync(x => x.OperationId == pending.OperationId, cancellationToken);
        }

        return await AllocateLinkAndDispatchAsync(
            owner,
            actorUserId,
            facts,
            tracked,
            context,
            provider,
            cancellationToken,
            snapshot);
    }

    private async Task<Result<ShippingCarrierShipmentOperationView>> AllocateLinkAndDispatchAsync(
        ShippingAuthoringOwnerScope owner,
        Guid actorUserId,
        OrderCommittedCarrierShipmentFacts facts,
        ShippingCarrierShipmentOperation tracked,
        ShippingCarrierAccountExecutionContext context,
        ICarrierShipmentProvider provider,
        CancellationToken cancellationToken,
        ExecutionSnapshot? existingSnapshot = null)
    {
        _ = owner;
        if (facts.FulfillmentId is not { } fulfillmentId)
        {
            return Fail(ShippingErrorCodes.CarrierShipmentNotEligible);
        }

        if (tracked.ExternalDispatchAttempted)
        {
            if (tracked.State == ShippingCarrierShipmentState.Pending)
            {
                tracked.MarkUnknown(DateTimeOffset.UtcNow, ShippingErrorCodes.CarrierShipmentUnknown);
                await db.SaveChangesAsync(cancellationToken);
            }

            return Result.Success(Map(tracked, provider.Capabilities, context));
        }

        var snapshot = existingSnapshot ?? ReadRequiredSnapshot(tracked);
        var selectedLines = snapshot.Lines;
        var sender = snapshot.Sender;
        var recipient = snapshot.Recipient;
        var package = snapshot.Package;

        Guid fulfillmentShipmentId;
        if (snapshot.FulfillmentShipmentId is { } linkedId && linkedId != Guid.Empty)
        {
            fulfillmentShipmentId = linkedId;
        }
        else
        {
            FulfillmentShipmentCreateResult allocated;
            try
            {
                var fulfillmentMethodCode = ResolveFulfillmentMethodCode(facts.MethodCode);
                allocated = await fulfillment.CreateShipmentAsync(
                    fulfillmentId,
                    actorUserId,
                    carrierDisplayName: facts.MethodLabel,
                    items: selectedLines.Select(x => new ShipmentLineCommand(x.OrderLineId, x.Quantity)).ToArray(),
                    shippingMethodCode: fulfillmentMethodCode,
                    providerMetadataJson: fulfillmentMethodCode is null
                        ? null
                        : BuildFulfillmentProviderMetadata(fulfillmentMethodCode, facts),
                    cancellationToken,
                    shippingGroupId: tracked.ShippingGroupId,
                    shippingDispatchKey: tracked.OperationKey);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (ContractOperationException exception)
            {
                return Fail(MapFulfillmentRefusal(exception.Code));
            }

            if (!TryMatchCreatedShipment(
                    allocated,
                    tracked.ShippingGroupId,
                    selectedLines,
                    out fulfillmentShipmentId))
            {
                return Fail(ShippingErrorCodes.CarrierShipmentNotEligible);
            }

            snapshot = snapshot with { FulfillmentShipmentId = fulfillmentShipmentId };
            tracked.SetExecutionInputSnapshot(SerializeExecutionSnapshot(snapshot));
            await db.SaveChangesAsync(cancellationToken);
        }

        var now = DateTimeOffset.UtcNow;
        var claimToken = UuidV7.New();
        var leaseUntil = now.Add(TimeSpan.FromSeconds(45));
        var supportsExecuteUpdate = db.Database.IsNpgsql();
        if (supportsExecuteUpdate)
        {
            // Atomic claim across concurrent DbContexts: only one UPDATE wins the lease.
            var claimed = await db.CarrierShipmentOperations
                .Where(x => x.OperationId == tracked.OperationId
                            && x.State == ShippingCarrierShipmentState.Pending
                            && !x.ExternalDispatchAttempted
                            && (x.DispatchClaimToken == null
                                || x.DispatchClaimToken == Guid.Empty
                                || x.DispatchClaimExpiresAt == null
                                || x.DispatchClaimExpiresAt <= now))
                .ExecuteUpdateAsync(
                    setters => setters
                        .SetProperty(x => x.DispatchClaimToken, claimToken)
                        .SetProperty(x => x.DispatchClaimExpiresAt, leaseUntil),
                    cancellationToken);
            if (claimed != 1)
            {
                await db.Entry(tracked).ReloadAsync(cancellationToken);
                return Result.Success(Map(tracked, provider.Capabilities, context));
            }

            // Persist "external may have started" BEFORE provider HTTP, outside any long DB transaction.
            var marked = await db.CarrierShipmentOperations
                .Where(x => x.OperationId == tracked.OperationId
                            && x.State == ShippingCarrierShipmentState.Pending
                            && x.DispatchClaimToken == claimToken
                            && !x.ExternalDispatchAttempted)
                .ExecuteUpdateAsync(
                    setters => setters
                        .SetProperty(x => x.ExternalDispatchAttempted, true)
                        .SetProperty(x => x.AttemptedAt, now)
                        .SetProperty(x => x.DispatchClaimExpiresAt, now.Add(TimeSpan.FromMinutes(5))),
                    cancellationToken);
            if (marked != 1)
            {
                await db.Entry(tracked).ReloadAsync(cancellationToken);
                return Result.Success(Map(tracked, provider.Capabilities, context));
            }

            await db.Entry(tracked).ReloadAsync(cancellationToken);
        }
        else
        {
            // InMemory / non-relational test hosts: entity claim + reload verify (no cross-context races in those suites).
            if (!tracked.TryAcquireDispatchClaim(claimToken, now, TimeSpan.FromSeconds(45)))
            {
                return Result.Success(Map(tracked, provider.Capabilities, context));
            }

            await db.SaveChangesAsync(cancellationToken);
            await db.Entry(tracked).ReloadAsync(cancellationToken);
            if (tracked.DispatchClaimToken != claimToken || tracked.ExternalDispatchAttempted)
            {
                return Result.Success(Map(tracked, provider.Capabilities, context));
            }

            tracked.MarkExternalDispatchAttempted(now);
            await db.SaveChangesAsync(cancellationToken);
            await db.Entry(tracked).ReloadAsync(cancellationToken);
            if (!tracked.ExternalDispatchAttempted || tracked.DispatchClaimToken != claimToken)
            {
                return Result.Success(Map(tracked, provider.Capabilities, context));
            }
        }

        CarrierShipmentResult outcome;
        try
        {
            var options = tipaxOptions.CurrentValue;
            outcome = await provider.CreateAsync(
                new CarrierShipmentCreateRequest(
                    tracked.OperationKey,
                    snapshot.AccountId,
                    snapshot.ConfigurationVersion,
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
                    snapshot.ProviderServiceCode,
                    new CarrierShipmentParty(
                        sender.FullName,
                        sender.Mobile,
                        sender.AddressLine,
                        snapshot.OriginLocationCode,
                        sender.PostalCode),
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
        }
        catch (OperationCanceledException)
        {
            if (tracked.State == ShippingCarrierShipmentState.Pending)
            {
                tracked.MarkUnknown(DateTimeOffset.UtcNow, ShippingErrorCodes.CarrierShipmentUnknown);
                await db.SaveChangesAsync(CancellationToken.None);
            }

            throw;
        }
        catch (Exception)
        {
            if (tracked.State == ShippingCarrierShipmentState.Pending)
            {
                tracked.MarkUnknown(DateTimeOffset.UtcNow, ShippingErrorCodes.CarrierShipmentUnknown);
                await db.SaveChangesAsync(CancellationToken.None);
            }

            throw;
        }

        ApplyCreateOutcome(tracked, outcome, DateTimeOffset.UtcNow);
        await db.SaveChangesAsync(cancellationToken);
        return Result.Success(Map(tracked, provider.Capabilities, context));
    }

    private static bool TryMatchCreatedShipment(
        FulfillmentShipmentCreateResult allocated,
        Guid shippingGroupId,
        IReadOnlyList<ShippingCarrierShipmentLineQuantity> selectedLines,
        out Guid createdShipmentId)
    {
        createdShipmentId = Guid.Empty;
        if (allocated.CreatedShipmentId == Guid.Empty)
        {
            return false;
        }

        var created = allocated.Snapshot.Shipments
            .SingleOrDefault(s => s.ShipmentId == allocated.CreatedShipmentId);
        if (created is null
            || created.Status != ShipmentOperationStatus.Created
            || created.ShippingGroupId != shippingGroupId)
        {
            return false;
        }

        if (created.Items.Count != selectedLines.Count)
        {
            return false;
        }

        foreach (var selected in selectedLines)
        {
            if (!created.Items.Any(i => i.OrderLineId == selected.OrderLineId && i.Quantity == selected.Quantity))
            {
                return false;
            }
        }

        createdShipmentId = allocated.CreatedShipmentId;
        return true;
    }

    private static string MapFulfillmentRefusal(string code)
        => code switch
        {
            var c when c.StartsWith("fulfillment.", StringComparison.Ordinal) =>
                ShippingErrorCodes.CarrierShipmentNotEligible,
            _ => ShippingErrorCodes.CarrierShipmentNotEligible,
        };

    /// <summary>
    /// Maps a committed Order method code onto the Fulfillment shipping-method registry code, or null when
    /// the carrier path should record only the display label (no registry/metadata validation).
    /// </summary>
    private static string? ResolveFulfillmentMethodCode(string? methodCode)
    {
        if (string.IsNullOrWhiteSpace(methodCode))
        {
            return null;
        }

        var trimmed = methodCode.Trim();
        if (Tooba.Fulfillment.Contracts.Shipping.ShippingMethodRegistry.Find(trimmed) is { } exact)
        {
            return exact.Code;
        }

        var head = trimmed.Split(':', 2)[0].Trim();
        return Tooba.Fulfillment.Contracts.Shipping.ShippingMethodRegistry.Find(head)?.Code;
    }

    private static string BuildFulfillmentProviderMetadata(
        string fulfillmentMethodCode,
        OrderCommittedCarrierShipmentFacts facts)
        => fulfillmentMethodCode switch
        {
            "tipax" => JsonSerializer.Serialize(new
            {
                recipientName = facts.RecipientName,
                recipientPhone = facts.RecipientMobile,
                fullAddress = facts.RecipientAddress,
                postalCode = facts.PostalCode,
                province = facts.ProvinceName,
                city = facts.CityName,
            }),
            "post" => JsonSerializer.Serialize(new
            {
                recipientName = facts.RecipientName,
                recipientPhone = facts.RecipientMobile,
                destinationAddress = facts.RecipientAddress,
                postalCode = facts.PostalCode,
            }),
            _ => "{}",
        };

    private async Task<ShippingCarrierShipmentOperation?> FindOpenCreateForGroupAsync(
        ShippingDraftOwner owner,
        Guid checkoutId,
        Guid shippingGroupId,
        CancellationToken cancellationToken)
    {
        var candidates = await db.CarrierShipmentOperations
            .Where(x => x.CheckoutId == checkoutId
                        && x.ShippingGroupId == shippingGroupId
                        && x.OwnerKind == owner.Kind
                        && x.OwnerTenantId == owner.TenantId
                        && x.OwnerSellerPartyId == owner.SellerPartyId
                        && x.Kind == ShippingCarrierShipmentKind.Create
                        && (x.State == ShippingCarrierShipmentState.Pending
                            || x.State == ShippingCarrierShipmentState.Unknown))
            .OrderByDescending(x => x.AttemptedAt)
            .Take(8)
            .ToListAsync(cancellationToken);
        return candidates.FirstOrDefault();
    }

    /// <summary>
    /// Crash/retry convergence without occurrence locator: only Pending/Unknown (never Succeeded).
    /// </summary>
    private async Task<ShippingCarrierShipmentOperation?> FindOpenCreateMatchingPayloadAsync(
        ShippingDraftOwner owner,
        Guid checkoutId,
        Guid shippingGroupId,
        string payloadFingerprint,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(payloadFingerprint))
        {
            return null;
        }

        var candidates = await db.CarrierShipmentOperations
            .Where(x => x.CheckoutId == checkoutId
                        && x.ShippingGroupId == shippingGroupId
                        && x.OwnerKind == owner.Kind
                        && x.OwnerTenantId == owner.TenantId
                        && x.OwnerSellerPartyId == owner.SellerPartyId
                        && x.Kind == ShippingCarrierShipmentKind.Create
                        && (x.State == ShippingCarrierShipmentState.Pending
                            || x.State == ShippingCarrierShipmentState.Unknown))
            .OrderByDescending(x => x.AttemptedAt)
            .Take(32)
            .ToListAsync(cancellationToken);
        foreach (var row in candidates)
        {
            if (TryReadExecutionSnapshot(row.ExecutionInputSnapshotJson, out var snapshot)
                && snapshot is not null
                && string.Equals(snapshot.PayloadFingerprint, payloadFingerprint, StringComparison.Ordinal))
            {
                return row;
            }
        }

        return null;
    }

    private async Task<ShippingCarrierShipmentOperation?> FindCreateByPackageDispatchIdAsync(
        ShippingDraftOwner owner,
        Guid checkoutId,
        Guid shippingGroupId,
        Guid packageDispatchId,
        CancellationToken cancellationToken)
    {
        var candidates = await db.CarrierShipmentOperations
            .Where(x => x.CheckoutId == checkoutId
                        && x.ShippingGroupId == shippingGroupId
                        && x.OwnerKind == owner.Kind
                        && x.OwnerTenantId == owner.TenantId
                        && x.OwnerSellerPartyId == owner.SellerPartyId
                        && x.Kind == ShippingCarrierShipmentKind.Create)
            .OrderByDescending(x => x.AttemptedAt)
            .Take(32)
            .ToListAsync(cancellationToken);
        foreach (var row in candidates)
        {
            if (TryReadExecutionSnapshot(row.ExecutionInputSnapshotJson, out var snapshot)
                && snapshot is not null
                && snapshot.PackageDispatchId == packageDispatchId)
            {
                return row;
            }
        }

        return null;
    }

    /// <summary>
    /// Same occurrence + different payload ⇒ typed conflict. Empty client payload skips the check (replay).
    /// </summary>
    private static bool TryDetectPayloadConflict(
        ShippingDraftOwner owner,
        ShippingCarrierShipmentOperation existing,
        ShippingCarrierShipmentExecuteRequest request,
        OrderCommittedCarrierShipmentFacts facts,
        out string? failureCode)
    {
        failureCode = null;
        if (!TryReadExecutionSnapshot(existing.ExecutionInputSnapshotJson, out var snapshot) || snapshot is null)
        {
            return false;
        }

        if (request.PackageDispatchId is { } locator
            && locator != Guid.Empty
            && locator != snapshot.PackageDispatchId)
        {
            failureCode = ShippingErrorCodes.CarrierShipmentIntentConflict;
            return true;
        }

        var hasClientPayload = (request.Lines is { Count: > 0 }) || request.Package is not null;
        if (!hasClientPayload || string.IsNullOrWhiteSpace(snapshot.PayloadFingerprint))
        {
            return false;
        }

        // Compare the client's stated line quantities to the bound fingerprint.
        // Do NOT re-check remaining executable qty here — that would refuse lawful locator
        // replay of a Succeeded occurrence after the group is exhausted (TB-249-R6).
        IReadOnlyList<ShippingCarrierShipmentLineQuantity> compareLines = snapshot.Lines;
        if (request.Lines is { Count: > 0 })
        {
            if (request.Lines.Any(line => line.OrderLineId == Guid.Empty || line.Quantity <= 0m))
            {
                failureCode = ShippingErrorCodes.CarrierShipmentIntentConflict;
                return true;
            }

            compareLines = request.Lines;
        }

        var comparePackage = request.Package is { } reqPkg
            ? new ShippingCarrierShipmentPackageInput(
                reqPkg.WeightGrams,
                0m,
                reqPkg.LengthCentimeters,
                reqPkg.WidthCentimeters,
                reqPkg.HeightCentimeters,
                0m)
            : snapshot.Package with { DeclaredValue = 0m, CollectAmount = 0m };

        if (!ShippingCarrierDispatchIntent.PayloadMatches(
                snapshot.PayloadFingerprint,
                owner,
                existing.CheckoutId,
                existing.ShippingGroupId,
                compareLines,
                comparePackage))
        {
            failureCode = ShippingErrorCodes.CarrierShipmentIntentConflict;
            return true;
        }

        return false;
    }

    private static bool TryResolveDispatchPhysicalPackage(
        ShippingCarrierShipmentExecuteRequest request,
        IReadOnlyList<ShippingCarrierShipmentLineQuantity> selected,
        OrderCommittedCarrierShipmentFacts facts,
        ShippingCarrierShipmentPackageInput groupPackage,
        out ShippingCarrierShipmentPackageInput physical,
        out string? failureCode)
    {
        failureCode = null;
        if (request.Package is { } dispatchPackage)
        {
            if (dispatchPackage.WeightGrams <= 0m
                || dispatchPackage.LengthCentimeters <= 0m
                || dispatchPackage.WidthCentimeters <= 0m
                || dispatchPackage.HeightCentimeters <= 0m)
            {
                physical = groupPackage;
                failureCode = ShippingErrorCodes.CarrierShipmentNotEligible;
                return false;
            }

            physical = new ShippingCarrierShipmentPackageInput(
                dispatchPackage.WeightGrams,
                0m,
                dispatchPackage.LengthCentimeters,
                dispatchPackage.WidthCentimeters,
                dispatchPackage.HeightCentimeters,
                0m);
            return true;
        }

        // Full remaining dispatch may reuse group-level completion/facts once; partial requires own facts.
        var selectsAllRemaining = facts.ExecutableLines.Count == selected.Count
            && facts.ExecutableLines.All(exec =>
                selected.Any(s => s.OrderLineId == exec.OrderLineId && s.Quantity == exec.RemainingPackedQuantity));
        if (!selectsAllRemaining)
        {
            physical = groupPackage;
            failureCode = ShippingErrorCodes.CarrierShipmentNotEligible;
            return false;
        }

        physical = new ShippingCarrierShipmentPackageInput(
            groupPackage.WeightGrams,
            0m,
            groupPackage.LengthCentimeters,
            groupPackage.WidthCentimeters,
            groupPackage.HeightCentimeters,
            0m);
        return physical.WeightGrams > 0m
               && physical.LengthCentimeters > 0m
               && physical.WidthCentimeters > 0m
               && physical.HeightCentimeters > 0m;
    }

    private async Task<(decimal Declared, decimal Collect)> SumPriorPackageMoneyAsync(
        ShippingDraftOwner owner,
        Guid checkoutId,
        Guid shippingGroupId,
        CancellationToken cancellationToken)
    {
        var rows = await db.CarrierShipmentOperations
            .AsNoTracking()
            .Where(x => x.CheckoutId == checkoutId
                        && x.ShippingGroupId == shippingGroupId
                        && x.OwnerKind == owner.Kind
                        && x.OwnerTenantId == owner.TenantId
                        && x.OwnerSellerPartyId == owner.SellerPartyId
                        && x.Kind == ShippingCarrierShipmentKind.Create
                        && (x.State == ShippingCarrierShipmentState.Succeeded
                            || x.State == ShippingCarrierShipmentState.Pending
                            || x.State == ShippingCarrierShipmentState.Unknown))
            .ToListAsync(cancellationToken);
        decimal declared = 0m;
        decimal collect = 0m;
        foreach (var row in rows)
        {
            if (!TryReadExecutionSnapshot(row.ExecutionInputSnapshotJson, out var snapshot) || snapshot is null)
            {
                continue;
            }

            declared += snapshot.Package.DeclaredValue;
            collect += snapshot.Package.CollectAmount;
        }

        return (declared, collect);
    }

    private static bool IsFinalRemainingDispatch(
        OrderCommittedCarrierShipmentFacts facts,
        IReadOnlyList<ShippingCarrierShipmentLineQuantity> selected)
        => facts.ExecutableLines.Count == selected.Count
           && facts.ExecutableLines.All(exec =>
               selected.Any(s => s.OrderLineId == exec.OrderLineId && s.Quantity == exec.RemainingPackedQuantity));

    private static bool OwnerMatches(ShippingDraftOwner owner, ShippingCarrierShipmentOperation row)
        => row.OwnerKind == owner.Kind
           && string.Equals(row.OwnerTenantId, owner.TenantId, StringComparison.Ordinal)
           && row.OwnerSellerPartyId == owner.SellerPartyId;

    private static string SerializeExecutionSnapshot(ExecutionSnapshot snapshot)
        => JsonSerializer.Serialize(new
        {
            snapshot.PackageDispatchId,
            snapshot.FulfillmentId,
            snapshot.FulfillmentShipmentId,
            Lines = snapshot.Lines.Select(x => new { x.OrderLineId, x.Quantity }).ToArray(),
            Sender = new
            {
                snapshot.Sender.FullName,
                snapshot.Sender.Mobile,
                snapshot.Sender.AddressLine,
                snapshot.Sender.PostalCode,
                snapshot.Sender.LocationCode,
            },
            Recipient = new
            {
                snapshot.Recipient.FullName,
                snapshot.Recipient.Mobile,
                snapshot.Recipient.AddressLine,
                snapshot.Recipient.PostalCode,
                snapshot.Recipient.LocationCode,
            },
            snapshot.OriginLocationCode,
            snapshot.ProviderServiceCode,
            Package = new
            {
                snapshot.Package.WeightGrams,
                snapshot.Package.DeclaredValue,
                snapshot.Package.LengthCentimeters,
                snapshot.Package.WidthCentimeters,
                snapshot.Package.HeightCentimeters,
                snapshot.Package.CollectAmount,
            },
            snapshot.AccountId,
            snapshot.ConfigurationVersion,
            snapshot.ProviderCode,
            snapshot.Currency,
            snapshot.PayloadFingerprint,
        });

    private ExecutionSnapshot ReadRequiredSnapshot(ShippingCarrierShipmentOperation tracked)
    {
        if (!TryReadExecutionSnapshot(tracked.ExecutionInputSnapshotJson, out var snapshot) || snapshot is null)
        {
            throw new ContractOperationException(ShippingErrorCodes.CarrierShipmentNotEligible);
        }

        return snapshot;
    }

    private static bool TryReadExecutionSnapshot(string? json, out ExecutionSnapshot? snapshot)
    {
        snapshot = null;
        if (string.IsNullOrWhiteSpace(json))
        {
            return false;
        }

        try
        {
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;
            if (!root.TryGetProperty("PackageDispatchId", out var dispatchEl)
                || !root.TryGetProperty("FulfillmentId", out var fulfillmentEl)
                || !root.TryGetProperty("Lines", out var linesEl)
                || !root.TryGetProperty("Sender", out var senderEl)
                || !root.TryGetProperty("Recipient", out var recipientEl)
                || !root.TryGetProperty("Package", out var packageEl)
                || !root.TryGetProperty("OriginLocationCode", out var originEl)
                || !root.TryGetProperty("ProviderServiceCode", out var serviceEl))
            {
                return false;
            }

            Guid? fulfillmentShipmentId = null;
            if (root.TryGetProperty("FulfillmentShipmentId", out var shipEl)
                && shipEl.ValueKind != JsonValueKind.Null
                && shipEl.TryGetGuid(out var shipId)
                && shipId != Guid.Empty)
            {
                fulfillmentShipmentId = shipId;
            }

            var lines = new List<ShippingCarrierShipmentLineQuantity>();
            foreach (var line in linesEl.EnumerateArray())
            {
                if (!line.TryGetProperty("OrderLineId", out var lineIdEl)
                    || !line.TryGetProperty("Quantity", out var qtyEl)
                    || !lineIdEl.TryGetGuid(out var lineId)
                    || !qtyEl.TryGetDecimal(out var qty))
                {
                    return false;
                }

                lines.Add(new ShippingCarrierShipmentLineQuantity(lineId, qty));
            }

            if (lines.Count == 0
                || !dispatchEl.TryGetGuid(out var packageDispatchId)
                || !fulfillmentEl.TryGetGuid(out var fulfillmentId)
                || !TryReadParty(senderEl, out var sender)
                || !TryReadParty(recipientEl, out var recipient)
                || !TryReadPackage(packageEl, out var package))
            {
                return false;
            }

            var accountId = root.TryGetProperty("AccountId", out var accountEl) && accountEl.TryGetGuid(out var aid)
                ? aid
                : Guid.Empty;
            var configurationVersion = root.TryGetProperty("ConfigurationVersion", out var verEl)
                                        && verEl.TryGetInt32(out var ver)
                ? ver
                : 0;
            var providerCode = root.TryGetProperty("ProviderCode", out var providerEl)
                ? providerEl.GetString() ?? string.Empty
                : string.Empty;
            var currency = root.TryGetProperty("Currency", out var currencyEl)
                ? currencyEl.GetString() ?? string.Empty
                : string.Empty;
            var payloadFingerprint = root.TryGetProperty("PayloadFingerprint", out var fpEl)
                ? fpEl.GetString() ?? string.Empty
                : string.Empty;

            snapshot = new ExecutionSnapshot(
                packageDispatchId,
                fulfillmentId,
                fulfillmentShipmentId,
                lines,
                sender!,
                recipient!,
                originEl.GetString() ?? string.Empty,
                serviceEl.GetString() ?? string.Empty,
                package!,
                accountId,
                configurationVersion,
                providerCode,
                currency,
                payloadFingerprint);
            return true;
        }
        catch (JsonException)
        {
            return false;
        }
    }

    private static bool TryReadParty(JsonElement el, out ShippingCarrierShipmentPartyInput? party)
    {
        party = null;
        if (!el.TryGetProperty("FullName", out var name)
            || !el.TryGetProperty("Mobile", out var mobile)
            || !el.TryGetProperty("AddressLine", out var address)
            || !el.TryGetProperty("LocationCode", out var location))
        {
            return false;
        }

        var postal = el.TryGetProperty("PostalCode", out var postalEl) ? postalEl.GetString() : null;
        party = new ShippingCarrierShipmentPartyInput(
            name.GetString() ?? string.Empty,
            mobile.GetString() ?? string.Empty,
            address.GetString() ?? string.Empty,
            location.GetString() ?? string.Empty,
            postal);
        return true;
    }

    private static bool TryReadPackage(JsonElement el, out ShippingCarrierShipmentPackageInput? package)
    {
        package = null;
        if (!el.TryGetProperty("WeightGrams", out var weight)
            || !el.TryGetProperty("DeclaredValue", out var declared)
            || !el.TryGetProperty("LengthCentimeters", out var length)
            || !el.TryGetProperty("WidthCentimeters", out var width)
            || !el.TryGetProperty("HeightCentimeters", out var height)
            || !el.TryGetProperty("CollectAmount", out var collect)
            || !weight.TryGetDecimal(out var weightV)
            || !declared.TryGetDecimal(out var declaredV)
            || !length.TryGetDecimal(out var lengthV)
            || !width.TryGetDecimal(out var widthV)
            || !height.TryGetDecimal(out var heightV)
            || !collect.TryGetDecimal(out var collectV))
        {
            return false;
        }

        package = new ShippingCarrierShipmentPackageInput(weightV, declaredV, lengthV, widthV, heightV, collectV);
        return true;
    }

    private sealed record ExecutionSnapshot(
        Guid PackageDispatchId,
        Guid FulfillmentId,
        Guid? FulfillmentShipmentId,
        IReadOnlyList<ShippingCarrierShipmentLineQuantity> Lines,
        ShippingCarrierShipmentPartyInput Sender,
        ShippingCarrierShipmentPartyInput Recipient,
        string OriginLocationCode,
        string ProviderServiceCode,
        ShippingCarrierShipmentPackageInput Package,
        Guid AccountId,
        int ConfigurationVersion,
        string ProviderCode,
        string Currency,
        string PayloadFingerprint = "");

    private static bool TryResolveLineSelection(
        OrderCommittedCarrierShipmentFacts facts,
        IReadOnlyList<ShippingCarrierShipmentLineQuantity>? requested,
        out IReadOnlyList<ShippingCarrierShipmentLineQuantity> selected,
        out string? failureCode)
    {
        failureCode = null;
        if (requested is null || requested.Count == 0)
        {
            selected = facts.ExecutableLines
                .Select(x => new ShippingCarrierShipmentLineQuantity(x.OrderLineId, x.RemainingPackedQuantity))
                .ToArray();
            return selected.Count > 0;
        }

        var remaining = facts.ExecutableLines.ToDictionary(x => x.OrderLineId, x => x.RemainingPackedQuantity);
        var resolved = new List<ShippingCarrierShipmentLineQuantity>();
        foreach (var line in requested)
        {
            if (line.OrderLineId == Guid.Empty || line.Quantity <= 0m)
            {
                failureCode = ShippingErrorCodes.CarrierShipmentNotEligible;
                selected = Array.Empty<ShippingCarrierShipmentLineQuantity>();
                return false;
            }

            if (!remaining.TryGetValue(line.OrderLineId, out var available) || line.Quantity > available)
            {
                failureCode = ShippingErrorCodes.CarrierShipmentNotEligible;
                selected = Array.Empty<ShippingCarrierShipmentLineQuantity>();
                return false;
            }

            remaining[line.OrderLineId] = available - line.Quantity;
            resolved.Add(new ShippingCarrierShipmentLineQuantity(line.OrderLineId, line.Quantity));
        }

        selected = resolved;
        return selected.Count > 0;
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

    public async Task<Result<ShippingGroupCarrierExecutionCapabilityView>> SaveSenderCompletionAsync(
        ShippingAuthoringOwnerScope owner,
        ShippingCarrierShipmentSenderCompletionRequest request,
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
        var existing = await db.CarrierShipmentSenderCompletions
            .SingleOrDefaultAsync(
                x => x.CheckoutId == request.CheckoutId
                     && x.ShippingGroupId == request.ShippingGroupId
                     && x.OwnerKind == domainOwner.Kind
                     && x.OwnerTenantId == domainOwner.TenantId
                     && x.OwnerSellerPartyId == domainOwner.SellerPartyId,
                cancellationToken);
        var now = DateTimeOffset.UtcNow;
        var location = facts.OriginLocationCode.Trim();
        if (existing is null)
        {
            db.CarrierShipmentSenderCompletions.Add(
                ShippingCarrierShipmentSenderCompletion.Create(
                    Guid.NewGuid(),
                    domainOwner.Kind,
                    domainOwner.TenantId,
                    domainOwner.SellerPartyId,
                    request.CheckoutId,
                    request.ShippingGroupId,
                    request.FullName,
                    request.Mobile,
                    request.AddressLine,
                    request.PostalCode,
                    location,
                    now));
        }
        else
        {
            db.CarrierShipmentSenderCompletions.Remove(existing);
            db.CarrierShipmentSenderCompletions.Add(
                ShippingCarrierShipmentSenderCompletion.Create(
                    existing.CompletionId,
                    domainOwner.Kind,
                    domainOwner.TenantId,
                    domainOwner.SellerPartyId,
                    request.CheckoutId,
                    request.ShippingGroupId,
                    request.FullName,
                    request.Mobile,
                    request.AddressLine,
                    request.PostalCode,
                    location,
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
        var views = new List<ShippingCarrierShipmentOperationView>(rows.Count);
        foreach (var row in rows)
        {
            var context = await ResolveAccountContextAsync(owner, row.AccountId, cancellationToken);
            views.Add(Map(row, ResolveCapabilities(row.ProviderCode), context));
        }

        return Result.Success<IReadOnlyList<ShippingCarrierShipmentOperationView>>(views);
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

        var context = await ResolveAccountContextAsync(owner, row.AccountId, cancellationToken);
        var capabilities = ResolveCapabilities(row.ProviderCode);
        return Result.Success(Map(row, capabilities, context));
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

        if (!providers.TryResolve(row.ProviderCode, out var provider) || provider is null)
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
        if (!ShippingCarrierOperationPermissions.CanTrackMutation(provider.Capabilities, context, row))
        {
            return Fail(context is null || string.IsNullOrWhiteSpace(context.Username) || !context.IsActive
                ? ShippingErrorCodes.CarrierUnconfigured
                : ShippingErrorCodes.CarrierShipmentNotEligible);
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

        return Result.Success(Map(row, provider.Capabilities, context));
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

        if (!providers.TryResolve(row.ProviderCode, out var provider) || provider is null)
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
        if (!ShippingCarrierOperationPermissions.CanCancelMutation(provider.Capabilities, context, row))
        {
            return Fail(context is null || string.IsNullOrWhiteSpace(context.Username) || !context.IsActive
                ? ShippingErrorCodes.CarrierUnconfigured
                : ShippingErrorCodes.CarrierShipmentNotEligible);
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

        return Result.Success(Map(row, provider.Capabilities, context));
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

    private async Task<ShippingCarrierAccountExecutionContext?> ResolveAccountContextAsync(
        ShippingAuthoringOwnerScope owner,
        Guid accountId,
        CancellationToken cancellationToken)
    {
        var domainOwner = ToDomainOwner(owner);
        return await accounts.TryResolveExecutionContextAsync(
            domainOwner.Kind,
            domainOwner.TenantId,
            domainOwner.SellerPartyId,
            accountId,
            cancellationToken);
    }

    private static ShippingCarrierShipmentOperationView Map(
        ShippingCarrierShipmentOperation row,
        CarrierShipmentCapabilities capabilities,
        ShippingCarrierAccountExecutionContext? context)
    {
        var createOp = row.Kind == ShippingCarrierShipmentKind.Create ? row : null;
        var permissions = ShippingCarrierOperationPermissions.ForCreateOperation(capabilities, context, createOp);
        var canTrack = permissions.CanTrack;
        var canCancel = permissions.CanCancel;
        Guid? packageDispatchId = null;
        if (TryReadExecutionSnapshot(row.ExecutionInputSnapshotJson, out var snapshot) && snapshot is not null)
        {
            packageDispatchId = snapshot.PackageDispatchId == Guid.Empty ? null : snapshot.PackageDispatchId;
        }

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
            row.CompletedAt,
            packageDispatchId);
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
