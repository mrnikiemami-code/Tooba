# TB-SHIPPING-MVP-RATE-COVERAGE-ADMIN-054 — Shipping rate & coverage authoring (program 5 of 6)

Status: **PARTIAL** — delivered: real, tested, compiling Shipping-owned rate/coverage authoring slice
(Contracts + Application + Infrastructure + Endpoints). Blocked: user-visible wiring in the running
Host and the `/admin/shipping-services` screen section, both of which require edits the task forbids
(`Program.cs`) or time beyond the HARD timebox.

## 1. Implemented (functional, not prepared-only)

### Contracts — `Tooba.Shipping.Contracts/Admin/ShippingAdminContracts.cs`

Write bodies (`ShippingRatePolicyWriteRequest`, `ShippingRatePolicyActiveRequest`,
`ShippingCoverageGrantWriteRequest`, `ShippingCoverageGrantActiveRequest`), read models
(`ShippingRatePolicyAdminItem`, `ShippingCoverageGrantAdminItem`) and the per-service view
`ShippingRatesAndCoverageView`. Destination scope is expressed with **stable Geography ids**
(`ShippingDestinationScopeWrite`) — never a translated label. New stable error codes added to
`ShippingErrorCodes`: `RatePolicyNotFound`, `CoverageGrantNotFound`, `RatePolicyScopeDuplicate`,
`CoverageGrantScopeDuplicate`, `ServiceCodeUnknown`.

### Application — new project `Tooba.Shipping.Application`

- Port `IShippingRateCoverageDirectory` (list / create rate / update rate / activate rate / create
  grant / activate grant) — the only data seam, implemented by Infrastructure, so Application never
  sees a DbContext.
- MediatR 12.5 messages + handlers: `CreateShippingRatePolicyCommand`,
  `UpdateShippingRatePolicyCommand`, `SetShippingRatePolicyActiveCommand`,
  `CreateShippingCoverageGrantCommand`, `SetShippingCoverageGrantActiveCommand`,
  `GetShippingRatesAndCoverageQuery`, all returning `Result<T>`.
- `ShippingAdminOperation.ExecuteAsync` maps the module's typed `SemanticException` to a stable
  `Result` failure (mirrors `SupportOperation`); no message-text matching.
- FluentValidation transport-shape validators (service-code presence/length, country required,
  service-area-without-city rejected, charge non-negative and within the domain maximum, three-letter
  currency, preparation/lead days 0..365, non-negative free-above threshold). Business invariants
  (uniqueness, existence, scope coherence) stay in Domain/Infrastructure and are **not** duplicated.

### Infrastructure — `Rates/ShippingRateCoverageDirectory.cs`

Real read/write over `ShippingDbContext` only (`shipping.rate_policies`, `shipping.coverage_grants`).
No Fulfillment/Geography DbContext, no cross-module join, no FK: the service code is an opaque
Contracts-compatible key and destinations are Geography ids. Behaviours:

- pre-write duplicate-scope check per service returning `…ScopeDuplicate` (the `NULLS NOT DISTINCT`
  unique index remains the second, authoritative guard);
- scope/service of an existing policy are immutable through this seam, so an update cannot silently
  re-point an existing commercial decision;
- soft activate/deactivate only — rows are never physically deleted;
- an empty coverage set is returned as an **empty set**: NO COVERAGE, never "unrestricted".
- registered in the module's own `ShippingModule.AddServices` (module-owned DI; no Host edit).

### Endpoints — new project `Tooba.Shipping.Endpoints`

`/v1/admin/shipping-services/{serviceCode}/rates…` and `…/coverage-grants…` mapped through
`ShippingEndpointModule.MapShippingEndpoints`, using `ISender` + `ApiResponseFactory` + an abstract
`IShippingAdminAuthorizer` (`shipping.rates.view` / `shipping.rates.manage` /
`shipping.coverage.manage`). Wire-only: no business logic, no `Results.Json`, no auth bypass.

## 2. Verified

- `Tooba.Shipping.Endpoints` (→ Application → Domain → Contracts) builds: **0 errors / 0 warnings**
  (`docs/architecture/evidence/…/log-build-shipping-endpoints.txt`).
- `Tooba.Shipping.Tests`: **69 passed / 0 failed** (63 pre-existing + 6 new in
  `Admin/ShippingRateCoverageAdminTests.cs`) covering create/update/deactivate roundtrip through a
  real `ShippingDbContext`, duplicate rate **and** duplicate grant denial with stable codes, empty
  coverage = no coverage, not-found codes, and all transport-shape denials
  (`docs/architecture/evidence/…/log-shipping-tests.txt`).
- No database write, no Host restart, no migration, no schema change, no `Program.cs`/`appsettings`
  change. Checkout price authority, Order snapshots and the shadow rate evaluator are untouched.

## 3. Blocked (exact seam for task 055)

1. **HTTP activation is impossible without `Program.cs`.** There is no auto-discovery seam: every
   module's endpoints are registered and mapped by explicit `Program.cs` lines
   (`builder.Services.AddSupportEndpointPresentation(); … app.MapSupportEndpoints();`) and MediatR
   scans assemblies listed in `builder.Services.AddToobaCqrsFoundation(...)`. The task allows
   `Host/Composition/**` "only if a VERIFIED canonical module-owned endpoint mapping exists" — the
   only such seam is `Program.cs`, which is explicitly forbidden, and the `Composition/` allowlist is
   frozen by `HostCompositionAmcGuardTests`. Therefore the routes are **not live** and the slice must
   not be presented as activated. 055 needs exactly: add the Host → `Tooba.Shipping.Endpoints`
   project reference, add Shipping's Application assembly to `AddToobaCqrsFoundation`,
   `builder.Services.AddShippingEndpointPresentation()` (a real Host-side `IShippingAdminAuthorizer`
   implementation, as Fulfillment/Support do), and `app.MapShippingEndpoints();`.
2. **The `/admin/shipping-services` screen section is not delivered.** Because the endpoints are not
   live, persisting from the UI would be a fake save; the task explicitly requires non-misleading
   controls rather than pretending. The existing screen, its grid, translations and option roundtrip
   tests were left byte-identical, and no parallel page or in-memory fake backend was introduced.
3. **`IShippingRateCoverageDirectory` is registered but currently has no HTTP consumer** — that is the
   only reason it is inert; it does not change any customer-facing behaviour.

## 4. Timebox honesty

Claim `2026-10-14T15:18:43Z` → result post ≈`2026-10-14T16:00Z`, i.e. ≈**41 minutes**: the 30-minute
HARD limit was exceeded while getting the slice to a compiling, green state; the code was not left
uncompiled, and no broad suite or long loop was run. The next task should be scoped as the activation +
UI task (item 3.1/3.2 above), not as another design step.
