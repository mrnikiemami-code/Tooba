# Host delta — TB-TMAR-ORDER-GOLDEN-001-R5-R1

## Removed from Host

| File | Role |
|------|------|
| `Host/Storefront/CheckoutAbuseGate.cs` | Abuse policy + OrderDbContext + CatalogDbContext + UtcNow |
| `Program.cs` DI `ICheckoutAbuseGate → Host.CheckoutAbuseGate` | Host registration |

## Moved / owned by Order

| Surface | Location |
|---------|----------|
| Abuse gate | `Order.Infrastructure/CheckoutAbuse/CheckoutAbuseGate.cs` |
| DI | `OrderModule` registers `ICheckoutAbuseGate` |
| Settings read | `Catalog.Contracts.Checkout.IStoreCheckoutAbuseSettingsReader` |
| Settings impl | `Catalog.Infrastructure/Checkout/StoreCheckoutAbuseSettingsReader.cs` |
| Clock | `IClock` (no `DateTimeOffset.UtcNow`) |

## Inventory conflict

`CheckoutProcessManager`: removed `InvalidOperationException.Message == "inventory.reservation.conflict"`. Only `ContractOperationException.Code == "inventory.reservation.conflict"`.

## Still remaining in Host for Order

- Admin order detail
- Admin CheckoutAbuseSettingsEndpoints (settings write UI — Catalog DbContext admin path; not storefront abuse enforcement)
- Thin storefront actor / checkout identity adapters (R5)
- ReservationCycleCoordinator / panel composers

## Allowed Host adapters

| Adapter | Reason |
|---------|--------|
| `HostOrderStorefrontActor` | session/auth only |
| `HostOrderStorefrontCheckoutIdentityGate` | thin wrap |
| Admin settings endpoints | Catalog admin write (out of R5-R1 storefront abuse scope) |

## SoT

- Order: `INCOMPLETE_REFERENCE_REPAIR`
- Checkout: `PAUSED_AT_SAFE_W5_CHECKPOINT`
- nextTask: `TB-TMAR-ORDER-GOLDEN-001-R6` (not started)
- Slice: `STOREFRONT_CHECKOUT_ABUSE_TYPED_R5_R1`
