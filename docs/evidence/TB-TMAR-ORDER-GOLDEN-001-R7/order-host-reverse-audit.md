# Host Order reverse audit — TB-TMAR-ORDER-GOLDEN-001-R7

**Mode:** AUDIT ONLY (no Host business removals / no panel / ReservationCycle / Checkout W6 / frontend migration)  
**HEAD at audit:** `6e9980a40c06a8ced0f4a32aefa004b700a99003` (== `origin/main`)  
**Inventory SoT:** `host-order-reference-inventory.json` (**38** Host production files)

## Closure decision

**`NOT_READY_FOR_CLOSURE`**

Order remains `INCOMPLETE_REFERENCE_REPAIR`. Illegal Host Order authority remains. Checkout stays `PAUSED_AT_SAFE_W5_CHECKPOINT`.

### Recommended residual slices

| Slice | Scope |
|-------|--------|
| **R8** | `ReservationCycleCoordinator` + `ReservationCyclePolicyResolver` + `UnpaidOrderExpiryHostedService` (Order cycle/expiry orchestration; replace `UtcNow` with `IClock`; Catalog settings via Contracts) |
| **R9** | CustomerPanel Order surfaces (list/detail/retry-unpaid + OrderDbContext composition + `/v1/customer/orders*` routes) |
| **R10** | SellerPanel Order surfaces (list/detail/dashboard Order counts + `/orders*` routes) |
| **R11** | Admin remaining Order dashboard/list/grids (`GetDashboard` / legacy `ListOrders` / sellers+customers Order counts + `AdminCustomersGridQueryEngine` / `AdminSellersGridQueryEngine` + Host `AdminReservationCycleMapper` cleanup) |

After R8–R11 + re-audit with zero `ILLEGAL_ORDER_AUTHORITY` → evaluate `READY_FOR_ORDER_COMPLETE_REFERENCE_PATTERN_CLOSURE` / `ORDER_CLOSURE_ONLY`.

**nextTask:** `TB-TMAR-ORDER-GOLDEN-001-R8`

## Classification legend (exact)

| Code | Meaning |
|------|---------|
| `ILLEGAL_ORDER_AUTHORITY` | Host owns Order business decisions, Order state writes, or Order use-cases that belong in Order CQRS/Endpoints |
| `ALLOWED_THIN_HOST_ADAPTER` | Legitimate Host composition: DI, migrate root, session/auth/port adapters |
| `NON_ORDER_HOST_CONCERN` | Incidental Order reference (guest actor constant, comments, Catalog settings, **dev seed**) without production Order authority |
| `SHARED_PANEL_READ_MODEL_NEEDS_OWNER_DECISION` | Panel/read-model coupling that needs Architect owner decision |

## Classification counts (row-level)

| Classification | Rows |
|----------------|------|
| ILLEGAL_ORDER_AUTHORITY | 22 |
| ALLOWED_THIN_HOST_ADAPTER | 12 |
| NON_ORDER_HOST_CONCERN | 15 |
| SHARED_PANEL_READ_MODEL_NEEDS_OWNER_DECISION | 5 |
| **Total rows** | **54** |

Inventory files covered: **38 / 38** (every discovered Host OrderDbContext / Order App|Infra|Domain / Order-route extra file has ≥1 row).

---

## Full audit table

| Host file | Type/method | Uses OrderDbContext? | Uses Order Application/Infrastructure/Domain? | Writes Order state? | Makes Order business decision? | Cross-module dependencies | Classification | Why | Required owner | Recommended next slice |
|-----------|-------------|----------------------|-----------------------------------------------|---------------------|--------------------------------|---------------------------|----------------|-----|----------------|------------------------|
| `ReservationCycleCoordinator.cs` | `ResolveForCheckoutAsync` | Yes | App + Domain + Infra | No | Yes | Order supply/policy ports | ILLEGAL_ORDER_AUTHORITY | Host resolves cycle policy for checkout via OrderDbContext lines | Order.Application | R8 |
| `ReservationCycleCoordinator.cs` | `EnsureRetryAfterExpiryAsync` | Yes | App + Domain + Infra | Yes (cycle directory + supply) | Yes (max cycles, reacquire, StartAsync) | OrderSupplyService, IReservationCycleDirectory | ILLEGAL_ORDER_AUTHORITY | Host owns unpaid-retry/reacquire + `DateTimeOffset.UtcNow` | Order.Application | R8 |
| `ReservationCycleCoordinator.cs` | `LoadPolicyLinesAsync` / `LoadReservationIdsAsync` | Yes | Domain + Infra | No | Yes | — | ILLEGAL_ORDER_AUTHORITY | OrderDbContext helpers for cycle orchestration | Order.Infrastructure | R8 |
| `ReservationCyclePolicyResolver.cs` | `ResolveAsync` / preview helpers | No | Application | No | Yes (tightest hold/cycle merge) | CatalogDbContext | ILLEGAL_ORDER_AUTHORITY | Order policy resolver in Host reading Catalog DbContext | Order.App + Catalog.Contracts | R8 |
| `UnpaidOrderExpiryHostedService.cs` | `ExecuteAsync` / `ReconcileOnceAsync` | No (ports) | Application + Domain (+ Contracts) | Yes (release + close cycle) | Yes | Payment gateway, IOrderPaymentProjectionPort, IReservationCycleDirectory | ILLEGAL_ORDER_AUTHORITY | Host worker owns unpaid expiry + cycle close + `UtcNow` | Order worker / Application | R8 |
| `Customer/CustomerPanelComposer.cs` | `ListOrdersAsync` | Yes | Domain + Infra | No | Yes | Payment | ILLEGAL_ORDER_AUTHORITY | Customer order list via OrderDbContext | Order Customer CQRS | R9 |
| `Customer/CustomerPanelComposer.cs` | `GetOrderAsync` | Yes | Domain + Infra + App supply models | No | Yes | Catalog, Party, Payment | ILLEGAL_ORDER_AUTHORITY | Customer order detail in Host | Order Customer CQRS | R9 |
| `Customer/CustomerPanelComposer.cs` | `RetryUnpaidAsync` | Yes (via GetOrder) | App Supply + Domain/Infra | Yes (via coordinator/supply) | Yes | ReservationCycleCoordinator, Payment | ILLEGAL_ORDER_AUTHORITY | Host decides unpaid retry/supply | Order command | R9 |
| `Customer/CustomerPanelComposer.cs` | `GetDashboardAsync` (Order-backed) | Yes | Domain + Infra | No | Yes (aggregates order states) | Wishlist, AddressBook | ILLEGAL_ORDER_AUTHORITY | Dashboard Order reads/counts via OrderDbContext | Order / panel owner | R9 |
| `Customer/CustomerPanelEndpoints.cs` | `GET/POST /v1/customer/orders*` | via composer | Application (guest constant on other handlers) | via retry | Yes | CustomerPanelComposer | ILLEGAL_ORDER_AUTHORITY | Host registers customer Order HTTP | Order.Endpoints | R9 |
| `Seller/SellerPanelComposer.cs` | `ListOrdersAsync` | Yes | Domain + Infra | No | Yes | AccessControl, Catalog | ILLEGAL_ORDER_AUTHORITY | Seller order list via OrderDbContext | Order Seller CQRS | R10 |
| `Seller/SellerPanelComposer.cs` | `GetOrderAsync` | Yes | Domain + Infra | No | Yes | AccessControl, Catalog | ILLEGAL_ORDER_AUTHORITY | Seller order detail via OrderDbContext | Order Seller CQRS | R10 |
| `Seller/SellerPanelComposer.cs` | `GetDashboardAsync` (Order counts) | Yes | Domain + Infra | No | Yes | Party | ILLEGAL_ORDER_AUTHORITY | Seller dashboard Order status aggregates | Order / panel | R10 |
| `Seller/SellerPanelEndpoints.cs` | `GET …/orders*` | via composer | No direct ns | No | Yes | SellerPanelComposer | ILLEGAL_ORDER_AUTHORITY | Host registers seller Order HTTP | Order.Endpoints | R10 |
| `Admin/AdminPanelComposer.cs` | `GetDashboardAsync` | Yes | Domain + Infra | No | Yes | Catalog, Offer.Contracts | ILLEGAL_ORDER_AUTHORITY | Direct OrderDbContext dashboard aggregation | Order | R11 |
| `Admin/AdminPanelComposer.cs` | `ListOrdersAsync` / `LoadOrderGroupsAsync` / `MapOrderListItemAsync` | Yes | App OrdersGrid + Domain + Infra | No | Yes | Returns.Contracts, Party | ILLEGAL_ORDER_AUTHORITY | Legacy Admin list still Host+OrderDbContext | Order | R11 |
| `Admin/AdminPanelComposer.cs` | `ListSellersAsync` | Yes | Infra | No | Yes (OrderCount) | Offer, Party | ILLEGAL_ORDER_AUTHORITY | Seller Order counts via OrderDbContext | Order | R11 |
| `Admin/AdminPanelComposer.cs` | `ListCustomersAsync` | Yes | Infra (+ Storefront names App) | No | Yes | — | ILLEGAL_ORDER_AUTHORITY | Customers derived from Order checkouts | Order | R11 |
| `Admin/AdminPanelEndpoints.cs` | `GET /v1/admin/orders` (`ListOrdersAsync`) | via composer | comment refs Order.Endpoints | No | Yes | AdminPanelComposer | ILLEGAL_ORDER_AUTHORITY | Host still owns legacy admin orders list route | Order.Endpoints | R11 |
| `Admin/AdminPanelEndpoints.cs` | dashboard/sellers/customers routes | via composer | No | No | Yes (Order-backed) | AdminPanelComposer | ILLEGAL_ORDER_AUTHORITY | Surfaces backed by illegal OrderDbContext methods | Order | R11 |
| `Grid/AdminCustomersGridQueryEngine.cs` | `QueryAsync` + checkout filters | Yes | Infra + Domain | No | Yes | — | ILLEGAL_ORDER_AUTHORITY | Customers grid is OrderDbContext-backed | Order | R11 |
| `Grid/AdminSellersGridQueryEngine.cs` | SellerOrders counts | Yes | Infra | No | Yes | Offer.Contracts, Party | ILLEGAL_ORDER_AUTHORITY | Sellers grid reads SellerOrders | Order | R11 |
| `Admin/AdminPanelComposer.cs` | `QuerySellersGridAsync` / `QueryCustomersGridAsync` | via engines | via engines | No | delegated | Grid engines | SHARED_PANEL_READ_MODEL_NEEDS_OWNER_DECISION | Thin facade over illegal Order-backed engines | Order vs Admin | R11 |
| `Admin/AdminReservationCycleMapper.cs` | Status/Reason/ToAudit/ToSummary | No | Application Detail/OrdersGrid/Supply + Domain | No | Yes (label/audit mapping facade) | — | SHARED_PANEL_READ_MODEL_NEEDS_OWNER_DECISION | Host still owns mapping facade over Order Application mappers for residual panels | Order (delete Host facade) | R8/R11 |
| `Admin/AdminPanelModels.cs` | residual panel DTOs | No | comment text only | No | No | — | SHARED_PANEL_READ_MODEL_NEEDS_OWNER_DECISION | Leftover Host models for dashboard/sellers/customers/receipts | Host/Order | R11 |
| `Admin/ReservationPolicyAdminEndpoints.cs` | reservation policy HTTP | No | Application | Catalog writes | Catalog policy admin | Catalog + resolver | SHARED_PANEL_READ_MODEL_NEEDS_OWNER_DECISION | Catalog policy admin coupled to Order.Application resolver types | Catalog + Order.Contracts | R8 companion |
| `Admin/ReservationPolicyAdminComposer.cs` | preview/map/validate | No | Application | Catalog writes | Catalog validation | Catalog | SHARED_PANEL_READ_MODEL_NEEDS_OWNER_DECISION | Composer uses Order.Application snapshots | Catalog + Order.Contracts | R8 companion |
| `CommerceHoldPolicy.cs` | `ICheckoutReservationHoldPolicy` (+ Cart/Payment) | No | Application interface | No | Platform hold resolve | CatalogDbContext, Payment holds | ALLOWED_THIN_HOST_ADAPTER | Host platform hold port adapter (Catalog for store holds) | Host | keep |
| `CheckoutReservationHoldPolicy.cs` | `ICheckoutReservationHoldPolicy` | No | Application interface | No | Hold hours from options | Payment gateway options | ALLOWED_THIN_HOST_ADAPTER | Options→port adapter (CommerceHoldPolicy wired in Program) | Host | keep |
| `Order/HostOrderStorefrontActor.cs` | `IOrderStorefrontActor` | No | Application ports | No | No | Host session | ALLOWED_THIN_HOST_ADAPTER | Thin session/dev-header adapter | Host | keep |
| `Order/HostOrderStorefrontActor.cs` | `HostOrderStorefrontCheckoutIdentityGate` | No | Application ports | No | No | CheckoutIdentityGate | ALLOWED_THIN_HOST_ADAPTER | Thin identity wrap | Host | keep |
| `Admin/HostOrderAdminAuthorizer.cs` | `IOrderAdminAuthorizer` | No | Order.Endpoints interface | No | No | AccessControl | ALLOWED_THIN_HOST_ADAPTER | Auth only | Host | keep |
| `Admin/HostOrderAdminEffectiveAccessReader.cs` | `IOrderAdminEffectiveAccessReader` | No | Application ports | No | No | AccessControl | ALLOWED_THIN_HOST_ADAPTER | Effective-access adapter | Host | keep |
| `Grid/AdminListGridPolicies.cs` | `Orders` policy | No | Application OrdersGrid.Models | No | No | BuildingBlocks.Grid | ALLOWED_THIN_HOST_ADAPTER | Grid policy helper for Order DTO | Host | keep/R11 optional |
| `Program.cs` | MediatR/DI Order ports + `MapOrderEndpoints` | No | Application (+ Endpoints) | No | No | composition root | ALLOWED_THIN_HOST_ADAPTER | Composition root | Host | keep |
| `Program.cs` | registers `ReservationCycleCoordinator` / resolver / `UnpaidOrderExpiryHostedService` | No | Application | Indirect | Indirect | — | ILLEGAL_ORDER_AUTHORITY | DI wires illegal Order orchestration into Host | Move with R8 | R8 |
| `Composition/ToobaModuleComposition.cs` | `new OrderModule()` | No | Infrastructure | No | No | — | ALLOWED_THIN_HOST_ADAPTER | Module registration | Host | keep |
| `Development/MarketplaceDevelopmentBootstrap.cs` | `MigrateAsync(OrderDbContext)` | Yes | Infra | schema only | No | many | ALLOWED_THIN_HOST_ADAPTER | Dev migrate composition | Host.Dev | keep |
| `Admin/ProductWorkspaceDevelopmentBootstrap.cs` | `MigrateAsync(OrderDbContext)` | Yes | Infra | schema only | No | many | ALLOWED_THIN_HOST_ADAPTER | Dev migrate composition | Host.Dev | keep |
| `Storefront/StorefrontEndpoints.cs` | leftover using/comments | No | Application using (unused) | No | No | Catalog identity | NON_ORDER_HOST_CONCERN | Leftover after R5; no Order business | Host cleanup | optional |
| `Admin/HoldPolicySettingsEndpoints.cs` | hold settings HTTP | No | Application (`IReservationCyclePolicyResolver`) | No Order rows | Catalog/settings | Catalog | NON_ORDER_HOST_CONCERN | Catalog/settings admin; Order App used for preview | Catalog | owner-decision |
| `Customer/CustomerPanelEndpoints.cs` | dashboard/profile/dev-context (non-order routes) | No | Application guest constant | No | No | CustomerPanelComposer | NON_ORDER_HOST_CONCERN | Panel shell; Order coupling via composer methods above | Host | R9 (orders only) |
| `Seller/SellerPanelComposer.cs` | `ListCatalogVariantsAsync` | No Order query | file usings Domain/Infra | No | No | Catalog, Party | NON_ORDER_HOST_CONCERN | Catalog listing in same composer file | Host/Catalog | — |
| `Seller/SellerPanelEndpoints.cs` | non-order seller routes | No | No | No | No | Catalog/Offer | NON_ORDER_HOST_CONCERN | Non-Order seller panel routes | Host | — |
| `AccessControl/AccessControlDevelopmentSeed.cs` | demo Order seed writes | Yes | App + Domain + Infra | Yes (dev only) | Dev demo data | AccessControl | NON_ORDER_HOST_CONCERN | Development seed writes Order for demos — not production Order authority | Host.Dev | — |
| `AddressBook/AddressBookDevelopmentSeed.cs` | guest actor constant | No | Application | No | No | AddressBook | NON_ORDER_HOST_CONCERN | Dev seed guest id | Host.Dev | — |
| `AddressBook/AddressBookEndpoints.cs` | guest actor helper | No | Application | No | No | AddressBook | NON_ORDER_HOST_CONCERN | Guest actor id constant | Host | — |
| `CustomerProfile/CustomerProfileDevelopmentSeed.cs` | guest actor constant | No | Application | No | No | CustomerProfile | NON_ORDER_HOST_CONCERN | Dev seed guest id | Host.Dev | — |
| `Customer/HostFulfillmentCustomerAuthorizer.cs` | guest constant + Order.Contracts ownership | No | Application constant + Contracts | No | No | Fulfillment, Order.Contracts | ALLOWED_THIN_HOST_ADAPTER | Thin customer authz | Host | keep |
| `Customer/HostNotificationCustomerAuthorizer.cs` | guest actor helper | No | Application | No | No | Notification | NON_ORDER_HOST_CONCERN | Guest actor id constant | Host | — |
| `Preferences/UserPreferenceEndpoints.cs` | guest actor helper | No | Application | No | No | Preferences | NON_ORDER_HOST_CONCERN | Guest actor id constant | Host | — |
| `Settings/SettingsFoundationDevelopmentSeed.cs` | guest actor constant | No | Application | No | No | Settings | NON_ORDER_HOST_CONCERN | Dev seed guest id | Host.Dev | — |
| `Support/SupportDevelopmentSeedHost.cs` | guest actor constant | No | Application | No | No | Support | NON_ORDER_HOST_CONCERN | Dev seed guest id | Host.Dev | — |
| `Wishlist/WishlistDevelopmentSeed.cs` | guest actor constant | No | Application | No | No | Wishlist | NON_ORDER_HOST_CONCERN | Dev seed guest id | Host.Dev | — |
| `Wishlist/WishlistEndpoints.cs` | guest actor helper | No | Application | No | No | Wishlist | NON_ORDER_HOST_CONCERN | Guest actor id constant | Host | — |

---

## Explicit verification (architect known illegals)

| Surface | Verdict |
|---------|---------|
| `ReservationCycleCoordinator` | **ILLEGAL** — OrderDbContext + retry/reacquire + max cycles + `UtcNow` |
| `ReservationCyclePolicyResolver` | **ILLEGAL** — Order policy in Host + Catalog DbContext (move with Catalog Contracts) |
| `UnpaidOrderExpiryHostedService` | **ILLEGAL** — Order cycle/expiry orchestration + `UtcNow` |
| `CustomerPanelComposer` Order surfaces | **ILLEGAL** — list/detail/retry/dashboard OrderDbContext |
| `SellerPanelComposer` Order surfaces | **ILLEGAL** — list/detail/dashboard OrderDbContext |
| `AdminPanelComposer` GetDashboard/ListOrders/ListSellers/ListCustomers + LoadOrderGroups/MapOrderListItem | **ILLEGAL** |
| `AdminCustomersGridQueryEngine` / `AdminSellersGridQueryEngine` | **ILLEGAL** |
| `AdminReservationCycleMapper` | **SHARED** — Host facade over Order Application mappers (cleanup with R8/R11) |
| `AccessControlDevelopmentSeed` OrderDbContext writes | **NON_ORDER_HOST_CONCERN** — dev seed only |
| Thin Host adapters / guest constants / Program DI / module registration / migrate bootstrap / hold port adapters | **ALLOWED** (as classified) |

### Host OrderDbContext files (complete)

1. `ReservationCycleCoordinator.cs`
2. `Admin/AdminPanelComposer.cs`
3. `Customer/CustomerPanelComposer.cs`
4. `Seller/SellerPanelComposer.cs`
5. `Grid/AdminCustomersGridQueryEngine.cs`
6. `Grid/AdminSellersGridQueryEngine.cs`
7. `AccessControl/AccessControlDevelopmentSeed.cs` (dev seed)
8. `Development/MarketplaceDevelopmentBootstrap.cs` (migrate)
9. `Admin/ProductWorkspaceDevelopmentBootstrap.cs` (migrate)

### Order routes still on Host

- `GET /v1/admin/orders` (list) — illegal residual (query+detail already Order)
- `GET/POST /v1/customer/orders…` — illegal residual
- `GET …/seller…/orders…` — illegal residual
- `app.MapOrderEndpoints()` — allowed composition

### Already migrated (preserved — not residual)

Admin OrdersGrid / Admin Order Detail+AdminViewAck / Storefront checkout|pending|shipping / CheckoutAbuseGate / Admin Ops|Completeness|Recovery-Supply Host removals remain intact.
