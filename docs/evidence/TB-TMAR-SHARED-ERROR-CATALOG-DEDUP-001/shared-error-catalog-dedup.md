# Canonical Error Catalog Duplicate-Code Closure — TB-TMAR-SHARED-ERROR-CATALOG-DEDUP-001

Parent: `TB-TMAR-ADDRESSBOOK-POST-REALIGN-REPAIR-001`
Channel: `tooba-main` · Worker: `tooba-worker-01` · Mode: `BACKEND_ONLY_UNTIL_EXPLICIT_RELEASE`
Track: `SHARED_FOUNDATION_REPAIR`
Parent commit: `67f2d4485ac78472c30650374fa01c81a410c2c4`

## 1. Problem

`ErrorDefinitionCatalog` is fail-fast: the composed Host catalog threw
`duplicate_error_descriptor:{code}`, so `IErrorDefinitionCatalog`, `ISafeErrorMapper`, and
`ApiResponseFactory` could not materialize at runtime. AddressBook final certification was blocked.

## 2. Complete duplicate audit (composed, 15 production contributors, 201 unique codes)

Probe composed: Foundation, Offer, Cart, Notification, Support, Wallet, Payment, Promotion, Order,
Pricing, AddressBook, Fulfillment, Identity, Returns, Settlement.

| # | Machine code | Previous contributors | Classification | HTTP |
| --- | --- | --- | --- | --- |
| 1 | `checkout.authentication_required` | Cart, Payment, Order | Forbidden (all) | 401 (all) |
| 2 | `customer.session.required` | Notification, Support, Wallet, Order, AddressBook | Forbidden (all) | 401 (all) |
| 3 | `seller.authorization.denied` | Support, Promotion, Order | Forbidden (all) | 403 (all) |
| 4 | `admin.authorization.denied` | Support, Wallet, Payment, Promotion | Forbidden (all) | 403 (all) |
| 5 | `payment.missing` | Payment, Order | NotFound (both) | 404 (both) |
| 6 | `payment.unpaid.supply_unavailable` | Payment, Order | Conflict (both) | 409 (both) |
| 7 | `inventory.reservation.retry_limit_reached` | Payment, Order | Conflict (both) | 409 (both) |
| 8 | `payment.rejected` | Payment, Order | Business (both) | 400 (both) |
| 9 | `customer.order.missing` | Order, Fulfillment | NotFound (both) | 404 (both) |
| 10 | `seller.order.missing` | Order, Fulfillment | NotFound (both) | 404 (both) |

Codes **9 and 10** were not in the original 8-code report; the generic audit surfaced them.
No description carried conflicting observable semantics (no `CONFLICTING_SEMANTICS`), so no
architecture/product STOP was required. Classification of every duplicate was
`IDENTICAL_SHARED_SEMANTICS` or `REDUNDANT_LOCAL_REGISTRATION`.

## 3. Ownership resolution

Rule applied: a code's canonical descriptor belongs to its **semantic / natural bounded context** —
the domain that defines the invariant, owns the retry/authorization policy, and is the primary
producer of the code. Localization resource ownership (`IErrorResourceSet.Owns()`) is deliberately
**not** the deciding factor: a resource set may resolve a key it does not own. Modules keep consuming
the same machine codes; they no longer re-register the descriptor.

| Code | Final canonical owner | Reason |
| --- | --- | --- |
| `checkout.authentication_required` | `FoundationErrorCatalogContributor` | cross-cutting auth boundary (Host identity gates), no semantic module owner; every module has its own local constant |
| `customer.session.required` | `FoundationErrorCatalogContributor` | cross-cutting session boundary (Host/customer-panel guards) |
| `seller.authorization.denied` | `FoundationErrorCatalogContributor` | cross-cutting role authorization (Host seller/admin access gates) |
| `admin.authorization.denied` | `FoundationErrorCatalogContributor` | cross-cutting role authorization (Host admin access gates) |
| `payment.missing` | `PaymentErrorCatalogContributor` | Payment bounded context; primary producer `StorefrontPaymentOrchestrator`/`PaymentAdminDirectory`/`PaymentWebhookHandler` |
| `payment.rejected` | `PaymentErrorCatalogContributor` | Payment bounded context; produced by `ProcessPaymentWebhookCommand` |
| `payment.unpaid.supply_unavailable` | `PaymentErrorCatalogContributor` | Payment bounded context; produced by `StorefrontPaymentOrchestrator`; Payment's `PaymentExceptionMapper` maps `inventory.supply.unavailable` → this code |
| `inventory.reservation.retry_limit_reached` | `OrderErrorCatalogContributor` | Order bounded context; `ReservationCycleOptions.MaxReservationCycles` policy + `ReservationCycleCoordinator`/`ReservationCycleDirectory` producer live in Order; Inventory has no retry concept |
| `customer.order.missing` | `OrderErrorCatalogContributor` | Order owns the order aggregate and `customer.*` keyspace |
| `seller.order.missing` | `OrderErrorCatalogContributor` | Order owns the order aggregate and `seller.*` keyspace |

No new cross-module Application/Infrastructure/Domain coupling was introduced: consumers reference
only the stable machine-code constants they already had (or the neutral `FoundationErrorCodes`),
never another module's implementation.

### 3.1 R1 bounded-context re-audit (5-point proof)

R0 initially assigned the `payment.*` descriptors to `OrderErrorCatalogContributor` because
`OrderErrorResourceSet.Owns()` claims those localization keys. R1 corrects that: resource ownership is
not bounded-context ownership.

| Code | 1. Semantic meaning | 2. Actual producers / consumers | 3. Natural owner | 4. Foundation? | 5. Not a convenience owner |
| --- | --- | --- | --- | --- | --- |
| `payment.missing` | A payment aggregate/record required to proceed does not exist | Producers: `StorefrontPaymentOrchestrator` (many), `PaymentAdminDirectory` (5), `PaymentWebhookHandler`, `GetStorefrontPaymentQuery`. Consumers: `StorefrontPendingPaymentService`, `AdminOrderOperationsOrchestrator` | Payment | No — a module-owned domain invariant exists | Payment defines the code in `PaymentErrorCodes` and is the dominant producer; Order only consumes it |
| `payment.rejected` | A payment attempt was rejected (provider/business decline) | Producer: `ProcessPaymentWebhookCommand`. Consumer: Payment exception mapping | Payment | No | Order has no producer; it only re-exposed a locale string |
| `payment.unpaid.supply_unavailable` | An unpaid order cannot be supplied/reserved for retry | Producers: `StorefrontPaymentOrchestrator`, Payment's `PaymentExceptionMapper` (`inventory.supply.unavailable` → this code). Consumers: Order retry command, pending surfaces | Payment | No | Payment originates/maps it; Order consumes the same stable code |
| `inventory.reservation.retry_limit_reached` | The per-store/category/offer max reservation re-cycle count was reached | Producer: `ReservationCycleCoordinator` + `ReservationCycleDirectory` (`RecordRetryLimitReachedAsync`); policy: `ReservationCycleOptions.MaxReservationCycles`. Payment also carries the constant | Order | No — Order is the true domain authority | The retry policy and event log are Order-owned; Inventory has **no** `IErrorCatalogContributor` and no retry concept, so Inventory is not the owner |
| `checkout.authentication_required` | Checkout requires an authenticated actor | Producers: Host `CheckoutIdentityGate`, `HostOrderStorefrontActor` | Foundation (platform auth boundary) | Yes — no module owns checkout authentication; every module keeps its own constant | Not centralized for convenience: no resource set owns it and no module defines the invariant |
| `customer.session.required` | A customer session is required | Producers: Host wishlist/reviews/customer-panel gates + AddressBook/Wallet/Support customer endpoints | Foundation (platform session boundary) | Yes — pure cross-cutting identity | Host-owned, not resource-driven |
| `seller.authorization.denied` | Seller role authorization denied | Producers: Host `SellerPanelAccess`, `SellerSettingsEndpoints`, `HostSupportSellerAuthorizer` | Foundation (platform role boundary) | Yes | Host-owned; Support/Promotion only carried local constants |
| `admin.authorization.denied` | Admin role authorization denied | Producers: Host `AdminPanelAccess`, `ContentAdminAccess`, `HostAdminPanelAccess`, `HostSettlementAdminAuthorizer`, wallet/support admin authorizers | Foundation (platform role boundary) | Yes | Host-owned; 4 modules carried local constants |

Consequence applied in R1: only the three `payment.*` descriptors moved from Order to
`PaymentErrorCatalogContributor`. `inventory.reservation.retry_limit_reached` stays Order-owned (it was
already Order-owned, now proven, not merely convenient). The four cross-cutting codes stay
Foundation-owned. `payment.missing` / `payment.rejected` / `payment.unpaid.supply_unavailable` keep the
same machine codes, classification, and HTTP status; `OrderErrorResourceSet` still resolves their
localization keys (so FA copy is unchanged).


## 4. Removed redundant registrations (minimum surface)

- `FoundationErrorCatalogContributor`: **added** the 4 shared cross-cutting descriptors (neutral owner).
  New `FoundationErrorCodes` holds the stable machine-code literals.
- `OrderErrorCatalogContributor`: removed `checkout.authentication_required`, `customer.session.required`,
  `seller.authorization.denied`, `admin.authorization.denied` (Order never registered `admin.*`); **R1**
  removed `payment.missing`, `payment.rejected`, `payment.unpaid.supply_unavailable` (moved to Payment).
  `inventory.reservation.retry_limit_reached` stays Order-owned.
- `CartErrorCatalogContributor`: removed `checkout.authentication_required`.
- `PaymentErrorCatalogContributor`: removed `inventory.reservation.retry_limit_reached` and the two
  cross-cutting codes (`admin.authorization.denied`, `checkout.authentication_required`); **R1**
  re-added the three Payment-owned descriptors (`payment.missing`, `payment.rejected`,
  `payment.unpaid.supply_unavailable`).
- `NotificationErrorCatalogContributor`: removed `customer.session.required`.
- `SupportErrorCatalogContributor`: removed `customer.session.required`, `seller.authorization.denied`,
  `admin.authorization.denied`.
- `WalletErrorCatalogContributor`: removed `customer.session.required`, `admin.authorization.denied`.
- `PromotionErrorCatalogContributor`: removed `seller.authorization.denied`, `admin.authorization.denied`.
- `AddressBookErrorCatalogContributor`: removed `customer.session.required`; `customer.address.missing`
  remains unique and canonical.
- `FulfillmentErrorCatalogContributor`: removed `customer.order.missing`, `seller.order.missing`.

Resource sets were **not** duplicated or moved: localization keeps resolving via the first
`IErrorResourceSet` that `Owns()` the key (Order's fa/en resources for `payment.*` /
`inventory.reservation.*` / `customer.order.*` / `seller.order.*` are unchanged — `payment.*` keys are
still resolved by `OrderErrorResourceSet` even though the *descriptor* is now Payment-owned) and via the
descriptor `SafeTitleFallback`/`platform.*` fallback for the cross-cutting codes. This is exactly why
descriptor ownership and localization ownership are tracked separately.

## 5. Result

```text
Duplicate-Effective-Descriptor-Count = 0
Error-Catalog-State                   = CANONICAL
Duplicate-Suppression-Mechanism       = NONE   (no first/last wins, no DistinctBy, no overwrite)
Machine-Code-Rename                   = NONE
HTTP-Semantic-Change                  = NONE
Localization-Regression               = NONE
Parallel-Catalog                      = ZERO
Composed catalog                      = boots with 201 unique codes
SafeErrorMapper (customer.session.required) = 401 (descriptor, not 500 fallback)
```

`ErrorDefinitionCatalog` fail-fast behavior, `IErrorCatalogContributor`, `IErrorResourceSet`, stable
machine codes, and correlation/trace/request-id behavior are all preserved unchanged.

## 6. Durable guard

`src/backend/Host/Tooba.Host.Tests/Architecture/ErrorCatalogUniqueCodeGuardTests.cs`

- reflects every public, non-abstract `IErrorCatalogContributor` shipped by `Tooba.*` production
  assemblies, composes them, and asserts **each machine code has exactly one owner** — generic, not a
  hard-coded list of today's 10 codes;
- materializes the real `ErrorDefinitionCatalog` and asserts registered count == unique count;
- asserts `SafeErrorMapper` resolves the shared cross-cutting codes to their descriptor HTTP status
  (401/403), not a generic 500.

## 7. Focused validation

| Validation | Result |
| --- | --- |
| Affected production builds (10 projects) | PASS, 0 errors |
| `Tooba.BuildingBlocks.Tests` | Passed 45 / Failed 0 |
| AddressBook canonical + physical guards + new catalog guard | Passed 17 / Failed 0 |
| Host tests reading Order contributor text | Passed 67 / Failed 0 |
| Cart / Support / Notification / Payment / Wallet / Promotion / Fulfillment tests | Passed 237 / Failed 0 |
| `ErrorCatalogUniqueCodeGuardTests` | Passed 2 / Failed 0 |

### 7.1 R1 focused re-validation

| Validation | Result |
| --- | --- |
| `PaymentErrorCatalogContributor` + `OrderErrorCatalogContributor` builds | PASS, 0 errors |
| `ErrorCatalogUniqueCodeGuardTests` (composed catalog, 0 duplicates) | Passed 2 / Failed 0 |
| `Tooba.BuildingBlocks.Tests` | Passed 45 / Failed 0 |
| `Tooba.Payment.Tests` | Passed 87 / Failed 0 |
| Host `CheckoutAbusePolicy` + `StorefrontPendingPayment` + `UnpaidOrderExpiry` + `AddressBook` | Passed 51 / Failed 0 (4 skipped) |
| `OrderEndpointPresentationTests` ownership assertions (updated) | Updated; `Tooba.Order.Tests` still blocked by pre-existing K3 |


## 8. Pre-existing, out of scope (not caused, not repaired)

- **K3**: `Tooba.Order.Tests` does not compile —
  `Architecture/OrderSellerPanelArchitectureGuardTests.cs` references
  `Tooba.AccessControl.Application.Models` / `.Permissions` without a `ProjectReference`
  (regressed by commit `681e3639`). Unrelated to the error catalog; left untouched per scope.

## 9. AddressBook blocker state

AddressBook's K1 blocker (catalog not runtime-bootable) is **closed**. AddressBook contributes only
`customer.address.missing` (unique, canonical); it consumes `customer.session.required` through the
shared Foundation owner without a duplicate registration. No general AddressBook recovery was reopened.

## 10. Recovery / SoT

`docs/architecture/tmar-current-state.json` → `sharedErrorCatalogDedup` records task id, duplicate
codes before/after, final canonical owner per code, removed registrations, validations, the closed
K1 blocker, the new K3 pre-existing note, and evidence path. `next Host folder started = false`.
