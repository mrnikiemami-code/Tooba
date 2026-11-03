using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Npgsql;
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

        // UNKNOWN/Pending package intent is reserved: reconcile the open create, never blind-resend.
        var openCreate = await db.CarrierShipmentOperations
            .Where(x => x.CheckoutId == request.CheckoutId
                        && x.ShippingGroupId == request.ShippingGroupId
                        && x.OwnerKind == domainOwner.Kind
                        && x.OwnerTenantId == domainOwner.TenantId
                        && x.OwnerSellerPartyId == domainOwner.SellerPartyId
                        && x.Kind == ShippingCarrierShipmentKind.Create
                        && (x.State == ShippingCarrierShipmentState.Pending
                            || x.State == ShippingCarrierShipmentState.Unknown))
            .OrderByDescending(x => x.AttemptedAt)
            .FirstOrDefaultAsync(cancellationToken);
        if (openCreate is not null)
        {
            if (openCreate.State == ShippingCarrierShipmentState.Unknown)
            {
                var unknownContext = await ResolveAccountContextAsync(owner, openCreate.AccountId, cancellationToken);
                return Result.Success(Map(openCreate, ResolveCapabilities(openCreate.ProviderCode), unknownContext));
            }

            return await ResumePendingCreateAsync(
                owner,
                actorUserId,
                facts,
                openCreate,
                cancellationToken);
        }

        var (allowed, failureCode, resolvedSender, resolvedPackage) = await readiness.EvaluateCreateAsync(
            owner,
            facts,
            requestedSender: null,
            cancellationToken);
        if (!allowed || resolvedSender is null || resolvedPackage is null)
        {
            return Fail(failureCode ?? ShippingErrorCodes.CarrierShipmentNotEligible);
        }

        if (facts.CarrierAccountId is not { } accountId
            || facts.ConfigurationVersion is not { } configurationVersion
            || string.IsNullOrWhiteSpace(facts.ProviderServiceCode)
            || facts.FulfillmentId is not { } fulfillmentId
            || facts.ExecutableLines.Count == 0
            || string.IsNullOrWhiteSpace(facts.DestinationLocationCode)
            || string.IsNullOrWhiteSpace(facts.OriginLocationCode))
        {
            return Fail(ShippingErrorCodes.CarrierShipmentNotEligible);
        }

        if (!TryResolveLineSelection(facts, request.Lines, out var selectedLines, out var lineFailure))
        {
            return Fail(lineFailure ?? ShippingErrorCodes.CarrierShipmentNotEligible);
        }

        var scaledPackage = ScalePackageForSelection(resolvedPackage, selectedLines, facts);
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

        if (!providers.TryResolve(context.ProviderCode, out var provider) || provider is null)
        {
            return Fail(ShippingErrorCodes.CarrierProviderUnknown);
        }

        if (!provider.Capabilities.CanCreate)
        {
            return Fail(ShippingErrorCodes.CarrierShipmentUnsupported);
        }

        // Durable package/dispatch identity BEFORE contested fulfillment allocation or provider HTTP.
        var packageDispatchId = UuidV7.New();
        var operationKey = $"create:{request.CheckoutId:N}:{request.ShippingGroupId:N}:{packageDispatchId:N}";
        var existing = await db.CarrierShipmentOperations
            .AsNoTracking()
            .SingleOrDefaultAsync(
                x => x.OperationKey == operationKey
                     && x.OwnerKind == domainOwner.Kind
                     && x.OwnerTenantId == domainOwner.TenantId
                     && x.OwnerSellerPartyId == domainOwner.SellerPartyId,
                cancellationToken);
        if (existing is not null)
        {
            var existingContext = await ResolveAccountContextAsync(owner, existing.AccountId, cancellationToken);
            return Result.Success(Map(existing, ResolveCapabilities(existing.ProviderCode), existingContext));
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
                facts.OriginLocationCode,
                facts.ProviderServiceCode,
                scaledPackage,
                context.AccountId,
                context.ConfigurationVersion,
                context.ProviderCode,
                facts.Currency)));
        db.CarrierShipmentOperations.Add(pending);
        try
        {
            // Pending + immutable execution snapshot in ONE SaveChanges before allocation/provider.
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException exception) when (IsUnique(exception))
        {
            db.Entry(pending).State = EntityState.Detached;
            var winner = await db.CarrierShipmentOperations
                .AsNoTracking()
                .SingleAsync(x => x.OperationKey == operationKey, cancellationToken);
            if (!OwnerMatches(domainOwner, winner))
            {
                return Fail(ShippingErrorCodes.CarrierShipmentNotFound);
            }

            var winnerContext = await ResolveAccountContextAsync(owner, winner.AccountId, cancellationToken);
            return Result.Success(Map(winner, provider.Capabilities, winnerContext));
        }

        var tracked = await db.CarrierShipmentOperations
            .SingleAsync(x => x.OperationId == pending.OperationId, cancellationToken);
        return await AllocateLinkAndDispatchAsync(
            owner,
            actorUserId,
            facts,
            tracked,
            selectedLines,
            resolvedSender,
            recipient,
            scaledPackage,
            context,
            provider,
            cancellationToken);
    }

    private async Task<Result<ShippingCarrierShipmentOperationView>> ResumePendingCreateAsync(
        ShippingAuthoringOwnerScope owner,
        Guid actorUserId,
        OrderCommittedCarrierShipmentFacts facts,
        ShippingCarrierShipmentOperation pending,
        CancellationToken cancellationToken)
    {
        if (!TryReadExecutionSnapshot(pending.ExecutionInputSnapshotJson, out var snapshot) || snapshot is null)
        {
            return Fail(ShippingErrorCodes.CarrierShipmentNotEligible);
        }

        var domainOwner = ToDomainOwner(owner);
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
            snapshot.Lines,
            snapshot.Sender,
            snapshot.Recipient,
            snapshot.Package,
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
        IReadOnlyList<ShippingCarrierShipmentLineQuantity> selectedLines,
        ShippingCarrierShipmentPartyInput sender,
        ShippingCarrierShipmentPartyInput recipient,
        ShippingCarrierShipmentPackageInput package,
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

        var snapshot = existingSnapshot ?? ReadRequiredSnapshot(tracked);
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
                // Committed method codes are often "tipax:standard" / "post:standard"; Fulfillment registry
                // owns the short codes and Tipax/Post metadata. CarrierApi HTTP remains Shipping-owned.
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
                    shippingGroupId: tracked.ShippingGroupId);
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

            tracked.SetExecutionInputSnapshot(SerializeExecutionSnapshot(snapshot with
            {
                FulfillmentShipmentId = fulfillmentShipmentId,
            }));
            await db.SaveChangesAsync(cancellationToken);
        }

        var options = tipaxOptions.CurrentValue;
        var outcome = await provider.CreateAsync(
            new CarrierShipmentCreateRequest(
                tracked.OperationKey,
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

    /// <summary>
    /// Physical weight/dims stay merchant package facts for this dispatch; declared value and collect
    /// scale by selected quantity share of group packed totals so partial packages do not each send full collect.
    /// </summary>
    private static ShippingCarrierShipmentPackageInput ScalePackageForSelection(
        ShippingCarrierShipmentPackageInput package,
        IReadOnlyList<ShippingCarrierShipmentLineQuantity> selected,
        OrderCommittedCarrierShipmentFacts facts)
    {
        var selectedQty = selected.Sum(x => x.Quantity);
        var denominator = facts.GroupPackedQuantityTotal is > 0m
            ? facts.GroupPackedQuantityTotal.Value
            : facts.ExecutableLines.Sum(x => x.RemainingPackedQuantity);
        if (selectedQty <= 0m || denominator <= 0m)
        {
            return new ShippingCarrierShipmentPackageInput(
                package.WeightGrams,
                0m,
                package.LengthCentimeters,
                package.WidthCentimeters,
                package.HeightCentimeters,
                0m);
        }

        var share = selectedQty / denominator;
        if (share > 1m)
        {
            share = 1m;
        }

        return new ShippingCarrierShipmentPackageInput(
            package.WeightGrams,
            RoundMoney(package.DeclaredValue * share),
            package.LengthCentimeters,
            package.WidthCentimeters,
            package.HeightCentimeters,
            RoundMoney(package.CollectAmount * share));
    }

    private static decimal RoundMoney(decimal value)
        => decimal.Round(value, 2, MidpointRounding.AwayFromZero);

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
                currency);
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
        string Currency);

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
