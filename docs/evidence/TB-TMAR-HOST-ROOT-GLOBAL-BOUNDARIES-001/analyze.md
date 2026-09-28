# TB-TMAR-HOST-ROOT-GLOBAL-BOUNDARIES-001 — Analyze

## Scope

Six root-level Host files were analyzed as one bounded recovery unit:

- CheckoutReservationHoldPolicy.cs
- CommerceHoldPolicy.cs
- GlobalUsings.SettlementApp.cs
- GlobalUsings.SettlementDomain.cs
- UnpaidOrderExpiryHostedService.cs
- UnpaidOrderExpiryHostOptions.cs

## Findings

### CheckoutReservationHoldPolicy

Host-owned business policy implementing Order Application's checkout hold port while reading Payment Infrastructure options. This is ILLEGAL_BUSINESS_AUTHORITY in Host and an Application/Infrastructure coupling leak.

Disposition: migrate responsibility out of Host. The Order-facing port remains Order-owned; a thin Order.Infrastructure adapter consumes the stable Payment.Contracts hold source.

### CommerceHoldPolicy

Host-owned cross-module payment-hold policy. It reads Catalog persistence directly and Payment Infrastructure/Application details while implementing Payment.Contracts and the Order checkout hold port.

Disposition: Payment owns hold-resolution semantics. Move the source to Payment.Infrastructure. Replace CatalogDbContext/Domain reach-through with a narrow Catalog.Contracts read seam named IStoreHoldPolicyHoursReader. Order consumes the Payment.Contracts source only through its own thin adapter.

### Settlement global usings

The two root global-using files inject Settlement Application/Infrastructure and Settlement Domain namespaces into all Host compilation units, hiding direct layer coupling.

Disposition: remove both files. No replacement global aliases/shims.

### UnpaidOrderExpiryHostedService + Options

The runtime loop is module-specific Order reservation/payment expiry orchestration. Business reconciliation already lives behind IUnpaidOrderExpiryReconciler.

Disposition: move worker shell and its options to Order.Infrastructure/ReservationCycle. Preserve config key Tooba:UnpaidOrderExpiry, worker name, metrics, logging, tenant loop, batch/poll semantics and neutral worker/context seams.

## Boundary design

- Payment -> Catalog: Tooba.Catalog.Contracts.Reservation.IStoreHoldPolicyHoursReader
- Order -> Payment: Tooba.Payment.Contracts.Hold.ICommerceHoldPolicySource
- Order worker -> platform: BuildingBlocks/Tooba.Persistence neutral worker/context abstractions
- Host: composition only; no six-file business implementation remains

## Protected behavior

- hold precedence: payment method > store > Payment:Gateway
- clamp ranges and defaults
- manual vs online timeout semantics
- initial expiry = max(online, manual)
- unpaid expiry worker name, metric, polling interval, batch size, per-tenant isolation, logging
- configuration section remains Tooba:UnpaidOrderExpiry
- no schema/migration/frontend changes
