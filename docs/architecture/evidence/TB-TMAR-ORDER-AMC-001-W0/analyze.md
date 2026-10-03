# TB-TMAR-ORDER-AMC-001-W0 — Analyze

## Mode

`ARCHITECT_DIRECT_AMSC` — Analyze only (no production code change in W0).

## Ownership

| Surface | Owner |
|---|---|
| Admin / Seller / Customer / Storefront Order HTTP | `Tooba.Order.Endpoints` |
| Capability CQRS (Checkout, Shipping, PendingPayment, Admin Operations/Grid/Detail/Completeness, Seller, Customer, ReservationCycle, …) | `Tooba.Order.Application` |
| Order aggregates / checkout / reservation-cycle rules | `Tooba.Order.Domain` |
| Directories / DbContext / Integrations / Persistence | `Tooba.Order.Infrastructure` |
| Cross-module ports (Payment/Fulfillment/Returns/Notifications/Admin reads) | `Tooba.Order.Contracts` |
| Host | Composition only (`OrderModule` registration + endpoint map) |

## Solution Explorer

- `/Modules/Order/` already groups Domain, Contracts, Application, Endpoints, Infrastructure, Tests in `Tooba.slnx` → `CANONICAL` (no slnx regrouping wave required).

## Foreign coupling / microservice blockers

- ProjectReference / `using` to foreign `Application|Infrastructure|Domain`: **ZERO** in production Order projects (guards already enforce).
- Legal Contracts-only outbound: Cart, Offer, Pricing, Inventory, Tax, Party, Catalog, Payment, Fulfillment, Returns, Settlement, Identity, OperatorProfile, AddressBook, Localization, Promotion (Infrastructure), AccessControl (Infrastructure), ModuleContracts, Persistence, BuildingBlocks.
- **Stale Domain → Offer.Contracts** project reference + unused `using Tooba.Offer.Contracts.Dtos` in `OrderDomain.cs` — remove in W1 (no Offer type used in Domain body; snapshots are primitives).
- Cross-module persistence joins: none found in Order production sources (integrations go through Contracts bridges).
- Microservice extractability: **blocked by structure (Domain root dump + god-file + Application over-foldering), error-code ownership (Application/Endpoints vs Contracts), residual `Results.Json` geography route, and oversized Admin orchestrator** — not by illegal App/Infra/Domain coupling.

## Structured state (W0)

| Field | State |
|---|---|
| Foundation-State | `FOUNDATION_PARTIAL` (projects exist; prior ARCH-COMPLETE-002 structureCertified=true is **not** COMPLETE_REFERENCE_PATTERN) |
| Ownership-State | correct (module-owned HTTP + CQRS) |
| File-Cohesion-State | `MULTI_RESPONSIBILITY_COHESION_VIOLATION` |
| Oversized/God-File-State | `OrderDomain.cs` ~888 LOC (enums+aggregates+events); `AdminOrderOperationsOrchestrator.cs` ~2446 LOC; `CheckoutDirectory.cs` ~863 LOC |
| Localization-State | `CANONICAL` for validators (`WithMessage` count = 0); Domain may contain Persian invariant exception text (legacy) |
| API-Result-Pattern-State | `PARTIAL` — endpoints inject `ApiResponseFactory`; one residual `Results.Json` static geography list in `StorefrontOrderEndpoints` |
| Stable-Error-Code-State | `PARTIAL` — codes live in Application (`*OrderErrors`) + Endpoints (`OrderErrorCodes`); contributor/resx in Endpoints; **no** `Contracts/Errors/` |
| Logging-State | `CANONICAL` (no parallel pipeline found) |
| CQRS-State | `COMPLIANT` for HTTP — ~73 `IRequest<>` lines; handlers via MediatR; thin endpoints |
| Validator-Coverage-State | `LIKELY_EXHAUSTIVE` (56 validator files; zero Persian `WithMessage`) — W3 must publish exhaustive matrix + durable guard |
| Contracts-Boundary-State | `CLEAN` (project refs) / ownership smell: stable error codes not in Contracts |
| Cross-Module-Coupling-State | `LEGAL_CONTRACTS_ONLY` (after W1 remove Domain→Offer.Contracts) |
| Cross-Module-Join-State | `NONE` |
| Persistence-Ownership-State | `CORRECT` (`OrderDbContext` module-owned) |
| Endpoint-Ownership-State | `MODULE_OWNED` |
| Host-Residue-State | composition/seed host only |
| Schema-Migration-State | `UNCHANGED` (no schema work in AMSC) |
| Behavior-Preservation-Risk | `MEDIUM` (structure moves + error catalog relocation must preserve codes/HTTP) |
| Structure-Handoff-State | `REQUIRED` |
| Folder-Granularity-State | `MIXED` — Application mostly capability-first but **7 single-file request leaves**; Domain `ROOT_DUMP`; Infra capability folders without `Directories/` / `DependencyInjection/` canon |
| Final-Disposition | `READY_TO_MIGRATE` |

## Root dump inventory

- **Domain** root: 10 files including god `OrderDomain.cs` — all flat `namespace Tooba.Order.Domain`
- **Application** root: empty of production dumps (good); errors still under audience folders; 7 over-foldered request leaves
- **Infrastructure** root: `OrderModule.cs` only (allowlist OK); capability trees under Admin/Checkout/Integrations/… (no `Directories/` canon yet)
- **Contracts**: capability folders present; **missing** `Errors/`
- **Endpoints** root: `OrderEndpointModule.cs` (allowlist OK)

## Single-file Application leaves (must flatten W1)

1. `Admin/Customers/Queries/ListAdminCustomers/`
2. `Admin/Dashboard/Queries/GetAdminOrderDashboardMetrics/`
3. `Admin/LegacyList/Queries/ListAdminOrders/`
4. `Admin/Sellers/Queries/GetSellerOrderCounts/`
5. `Customer/Queries/GetCustomerOrderDashboardSummary/`
6. `Seller/Queries/GetSellerOrderDashboardSummary/`
7. `Storefront/PendingPayment/Queries/ListStorefrontPendingPayments/`

## Residual non-canonical HTTP

| File | Issue |
|---|---|
| `Storefront/StorefrontOrderEndpoints.cs` | `Results.Json(StorefrontIranGeography.Provinces)` on geography route |

## Wave plan

| Wave | Focus | Commit |
|---|---|---|
| W0 | Analyze + SoT start | docs only |
| W1 | Split Domain god-file + Domain root → Aggregates/Enums/Events/Checkout/Reservation/…; flatten 7 Application leaves; remove Domain→Offer.Contracts; path↔namespace EXACT; GlobalUsings | code |
| W2 | Infrastructure → `Directories/` + `DependencyInjection/OrderModule`; keep Integrations/Adapters cohesion; VS Solution Explorer professional | code |
| W3 | Move stable error codes → `Contracts/Errors`; register catalog from Infrastructure; fix geography `ApiResponseFactory`; exhaustive validator matrix + durable guards; classify/split Admin orchestrator if MULTI_RESPONSIBILITY | code |
| W4 | Structure gate + Certify `COMPLETE_REFERENCE_PATTERN` + durable guards + manifest/SoT | code+docs |

## Canonical reference

- Offer / BuildingBlocks for Result, `ApiResponseFactory`, CQRS foundation, error catalog, path↔namespace
- Catalog AMSC W0–W4 for COMPLETE_REFERENCE_PATTERN evidence shape
- Party / UserPreference for `/Modules/<Name>/` certify pattern

## Microservice readiness target

Order must extract with **zero** foreign Application/Infrastructure/Domain references, Contracts-only composition, module-owned persistence schema, and Host as composition root only. W1–W4 close residual platform-kernel smells that would force co-deployment today.
