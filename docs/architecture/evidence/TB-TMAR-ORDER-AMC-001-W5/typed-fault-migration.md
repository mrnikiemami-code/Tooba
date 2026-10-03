# TB-TMAR-ORDER-AMC-001-W5 — Typed contract-fault migration (W4 blocker closure)

## Purpose

W4 returned `NOT_CERTIFIED` for one reason only: expected-failure **classification by parsing
`InvalidOperationException.Message`** still existed in the Order surface, with no canonical lock exemption.

W5 removes the *cause* of that classification instead of legalising it:

- expected business faults are now **produced** as the canonical typed fault
  `Tooba.BuildingBlocks.ContractOperationException(code)`; and
- consumers **classify by `ex.Code`**, never by `ex.Message`.

No route, status code, response shape, public Contracts type, schema, or migration changed. The observable
stable machine code of every migrated flow is byte-for-byte preserved; only the **carrier** changed
(untyped `Message` string → typed `Code` property).

## Canonical mechanism (existing, not invented)

```startLine:7:19:src/backend/BuildingBlocks/Tooba.BuildingBlocks/ContractOperationException.cs
public sealed class ContractOperationException : Exception
{
    /// <summary>Creates a typed contract-boundary failure.</summary>
    public ContractOperationException(string code, Exception? innerException = null)
        : base(code, innerException)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(code);
        Code = code;
    }

    /// <summary>Stable semantic error code (property — not Message-based classification).</summary>
    public string Code { get; }
}
```

`Code` is a property, so classification can never silently fall back to prose.

## B1 closure — storefront consumer

`src/backend/Modules/Order/Tooba.Order.Application/Storefront/Services/StorefrontOrderResult.cs`

| Before (W4) | After (W5) |
|---|---|
| `catch (InvalidOperationException exception) when (TryMapCheckoutDirectoryCode(exception.Message, out var code))` | `catch (ContractOperationException exception) when (TryMapCheckoutCode(exception.Code, out var code))` |
| `TryMapCheckoutDirectoryCode` split the message string | `TryMapCheckoutCode` matches the typed `Code` only |

The prefix mapping (`inventory.`, `PRICE_CHANGED`, `PROMOTION_CHANGED`, `TAX_*`, `checkout.`, `shipping.`,
`order.`, `pending.`, `payment.`) is preserved 1:1 — it is now applied to a typed code, not to prose.

## B2 closure — admin fulfillment consumer

`src/backend/Modules/Order/Tooba.Order.Infrastructure/Admin/Fulfillment/AdminOrderFulfillmentOperations.cs`

| Before (W4) | After (W5) |
|---|---|
| `catch (InvalidOperationException ex) when (TryMapStableMachineCode(ex.Message, out var mapped))` | `catch (ContractOperationException ex) when (TryMapStableMachineCode(ex.Code, out var mapped))` |

The preserved transition remap `fulfillment.cancel.already_dispatched → fulfillment.dispatch.already_dispatched`
is now keyed on `Code`. Because the carrier is typed, Order no longer classifies the **foreign** Fulfillment
`shipping_service.*` text — that dependency is gone.

## B3 closure — producers now emit the typed fault

| File | Codes migrated to `ContractOperationException(code)` |
|---|---|
| `Infrastructure/Checkout/Persistence/CheckoutDirectory.Reservations.cs` | `inventory.supply.unavailable`, `PRICE_CHANGED`, `TAX_NO_APPLICABLE_RULE`, `TAX_CALCULATION_ERROR`, `PROMOTION_CHANGED`, `checkout.offer.*`, `checkout.price.*` |
| `Infrastructure/Checkout/Persistence/CheckoutDirectory.cs` | `order.cancel.forbidden`, `order.restore.invalid_state`, `checkout.cart.missing`, `checkout.cart.expired`, `checkout.cart.empty` |
| `Infrastructure/Checkout/Persistence/CheckoutSubmitHost.cs` | `checkout.missing`, `checkout.cart.missing`, `checkout.cart.expired`, `checkout.version.conflict`, `checkout.cart.empty` |
| `Infrastructure/Integrations/Payment/OrderUnpaidRetrySupplyBridge.cs` | `payment.unpaid.supply_unavailable`, `inventory.reservation.retry_limit_reached` |
| `Infrastructure/Admin/Supply/OrderSupplyCheckoutStore.cs` | `order.operation.invalid` |
| `Domain/Aggregates/SellerOrder.cs` | `order.seller_order.empty`, `order.cancel.forbidden`, `order.restore.invalid_state`, `order.restore.missing_snapshot`, `order.payment.confirm.invalid_state`, `order.payment.unconfirm.invalid_state` |
| `Domain/Checkout/CartShippingDraft.cs` | `shipping.note.too_long` |
| `Domain/Checkout/CheckoutProcess.cs` | `checkout_process.*` |

Genuine invariant violations (programmer errors, unsupported DI seam, outbox mapping) intentionally remain
plain `InvalidOperationException` — they are not expected business faults and are not classified anywhere.

## Cross-module producers (needed so Order/other consumers never parse text)

| File | Change |
|---|---|
| `Modules/Fulfillment/Tooba.Fulfillment.Application/Shipping/ShippingMethodRegistry.cs` | `ShippingProviderMetadataValidator` throws `ContractOperationException` for `fulfillment.shipping*` |
| `Modules/Payment/Tooba.Payment.Application/Orchestration/StorefrontPaymentOrchestrator.cs` | `RetryUnpaidCoreAsync` catches `ContractOperationException` and maps by `Code` |

Fulfillment/Payment use their own module-internal `*ExceptionMapper` (their own module's fault-to-code
mechanism, unchanged by this task). Order never consumes those mappers; the Order/Fulfillment boundary is
`Contracts` only.

## Proof — zero message classification in the certified surface

Production scan of `Modules/Order` (`bin`/`obj`/`Migrations`/tests excluded) for
`catch (...) when (...Message...)`, `.Message.StartsWith/Contains/==`:

```text
hits = 0
```

All ten touched production files: `ContractOperationException` present, `Message` references = **0**.

## Architecture-guard aggregation

Three god-files split in W3 (`CheckoutDirectory`, `AdminOrderOperationsOrchestrator`, `AdminOrderHistoryComposer`)
mean guards must assert the whole **partial family**, not one shard. `Tooba.Host.Tests/OrderPartialSources.cs`
adds `ReadAll` / `ReadAllAbsolute`, which concatenate a partial file family in stable order. Guards were
repointed to it; no assertion was weakened or removed.
