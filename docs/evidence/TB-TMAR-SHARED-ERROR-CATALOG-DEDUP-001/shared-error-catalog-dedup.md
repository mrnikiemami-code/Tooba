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

Rule applied: the canonical descriptor is the one whose module owns the code's localization
keyspace (the only `IErrorResourceSet` that `Owns()` the key) plus the domain that raises it. Modules
keep consuming the same machine codes; they no longer re-register the descriptor.

| Code | Final canonical owner | Reason |
| --- | --- | --- |
| `checkout.authentication_required` | `FoundationErrorCatalogContributor` | cross-cutting auth boundary; no module resource set owns `checkout.*` for auth |
| `customer.session.required` | `FoundationErrorCatalogContributor` | cross-cutting session boundary |
| `seller.authorization.denied` | `FoundationErrorCatalogContributor` | cross-cutting role authorization |
| `admin.authorization.denied` | `FoundationErrorCatalogContributor` | cross-cutting role authorization |
| `payment.missing` | `OrderErrorCatalogContributor` | `OrderErrorResourceSet.Owns("payment.missing")`; raised on storefront/pending surfaces |
| `payment.rejected` | `OrderErrorCatalogContributor` | `OrderErrorResourceSet.Owns("payment.rejected")` |
| `payment.unpaid.supply_unavailable` | `OrderErrorCatalogContributor` | `OrderErrorResourceSet.Owns(...)`, with FA resource |
| `inventory.reservation.retry_limit_reached` | `OrderErrorCatalogContributor` | `OrderErrorResourceSet.Owns(...)`, `ReservationCycleErrors` lives in Order |
| `customer.order.missing` | `OrderErrorCatalogContributor` | Order owns `customer.*` keyspace and the order aggregate |
| `seller.order.missing` | `OrderErrorCatalogContributor` | Order owns `seller.*` keyspace and the order aggregate |

No new cross-module Application/Infrastructure/Domain coupling was introduced: consumers reference
only the stable machine-code constants they already had (or the new neutral
`FoundationErrorCodes`), never another module's implementation.

## 4. Removed redundant registrations (minimum surface)

- `FoundationErrorCatalogContributor`: **added** the 4 shared cross-cutting descriptors (neutral owner).
  New `FoundationErrorCodes` holds the stable machine-code literals.
- `OrderErrorCatalogContributor`: removed `checkout.authentication_required`, `customer.session.required`,
  `seller.authorization.denied`, `admin.authorization.denied` (Order never registered `admin.*`).
- `CartErrorCatalogContributor`: removed `checkout.authentication_required`.
- `PaymentErrorCatalogContributor`: removed `inventory.reservation.retry_limit_reached`,
  `payment.missing`, `payment.rejected`, `payment.unpaid.supply_unavailable`,
  `admin.authorization.denied`, `checkout.authentication_required`.
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
`inventory.reservation.*` / `customer.order.*` / `seller.order.*` are unchanged) and via the
descriptor `SafeTitleFallback`/`platform.*` fallback for the cross-cutting codes.

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
