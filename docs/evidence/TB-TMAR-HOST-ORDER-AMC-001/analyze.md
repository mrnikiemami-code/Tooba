# Analyze — Host/Order AMC-001

## Target

`src/backend/Host/Tooba.Host/Order/` (1 file: `HostOrderStorefrontActor.cs`)

## Inventory

| Type | Role |
|---|---|
| `HostOrderStorefrontActor` | Thin session / Dev-Testing header / guest → `IOrderStorefrontActor` |
| `HostOrderStorefrontCheckoutIdentityGate` | Thin wrap of `CheckoutIdentityGate` → `IOrderStorefrontCheckoutIdentityGate` |

## Ownership verdict

| Slice | Disposition |
|---|---|
| Entire `Host/Order` | **KEEP_AS_THIN_HOST_ORDER_STOREFRONT_ADAPTER** (not HOST_ZERO) |

HTTP / CQRS / schema already module-owned by Order. Host retains only platform session→actor adapters (same class as Security Payment storefront authorizer).

## Coupling / repairs

1. Guest id previously referenced `Order.Application` `StorefrontCheckoutService` → **repaired** to `Order.Contracts.StorefrontGuestActor`.
2. Auth code previously used `StorefrontOrderErrors` → **repaired** to `FoundationErrorCodes.CheckoutAuthenticationRequired` while keeping `StorefrontOrderException` for Result mapping parity.
3. Allowed seam: Order.Application **Ports** + typed `StorefrontOrderException` only; Domain/Infrastructure/Persistence = ZERO.
4. Path↔namespace = `EXACT` `Tooba.Host.Order`.

## Must evacuate

None under current TMAR locks.
