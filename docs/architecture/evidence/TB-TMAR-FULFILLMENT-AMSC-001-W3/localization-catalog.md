# TB-TMAR-FULFILLMENT-AMSC-001 — W3 localization & error-catalog detail

## Count truth

| Metric | Value |
| --- | --- |
| `FulfillmentErrorCodes` declared constants | **86** |
| `FulfillmentErrorCatalogContributor` registered descriptors | **84** |
| Order-owned codes declared/consumed but **not** re-registered | **2** (`seller.order.missing`, `customer.order.missing`) |
| `FulfillmentErrors.resx` keys | **86** |
| `FulfillmentErrors.fa.resx` keys | **86** |
| Declared codes missing a resx key | **0** |
| Duplicate descriptor ownership | **0** |
| Reachable unregistered Fulfillment-owned codes | **0** |

## Descriptor ownership

Fulfillment is the single owner of the `fulfillment.*`, `shipping_service.*` and
`shipping_service_option.*` keyspaces plus the seller/customer authorization codes it emits at its own
boundary (`seller.order.handle.denied`, `seller.order.handle.scope_denied`, `customer.actor.missing`).

`seller.order.missing` and `customer.order.missing` are declared in `FulfillmentErrorCodes` (the module
consumes them) but their descriptors are owned by the Order contributor
(`OrderErrors.resx`/`OrderErrors.fa.resx` already ship both keys). Fulfillment must **not** re-register
them; the composed catalog is fail-fast on duplicates. W1 removed the previous duplicate ownership.

## Resource set

`Tooba.Fulfillment.Endpoints/Resources/FulfillmentErrorResources.cs`:

```csharp
public bool Owns(string localizationKey) =>
    localizationKey.StartsWith("fulfillment.", StringComparison.OrdinalIgnoreCase)
    || localizationKey.StartsWith("shipping_service", StringComparison.OrdinalIgnoreCase)
    || localizationKey.Equals("seller.order.handle.denied", StringComparison.OrdinalIgnoreCase)
    || localizationKey.Equals("seller.order.handle.scope_denied", StringComparison.OrdinalIgnoreCase)
    || localizationKey.Equals("customer.actor.missing", StringComparison.OrdinalIgnoreCase);
```

Registered as `IErrorResourceSet` in `FulfillmentEndpointModule.AddFulfillmentEndpointPresentation`;
the catalog contributor is registered in
`Tooba.Fulfillment.Infrastructure/DependencyInjection/FulfillmentModule.cs`.

## Localization state

`MISSING_INFRASTRUCTURE_USE` (W0) → `CATALOGUED_86_KEYS_IN_BOTH_CULTURES` (W1) → `CANONICAL` (W3).

Every declared stable code resolves to a localized string in both `en` and `fa`; the catalog descriptor
carries `LocalizationKey = code` and a safe English fallback. No user-facing error falls back to a raw
`SafeTitleFallback`.
