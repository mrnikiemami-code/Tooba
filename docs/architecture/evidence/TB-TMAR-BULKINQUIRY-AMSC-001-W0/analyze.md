# TB-TMAR-BULKINQUIRY-AMSC-001-W0 — Analyze

## Mode

`ARCHITECT_DIRECT_AMSC` — Analyze only. No production code moved in this wave.

Baseline: `branch = main`, `HEAD == origin/main == 2127591e`.

## Target analyzed

`src/backend/Modules/BulkInquiry` (5 projects) + its Host composition seams + its AMC-001 evidence/guards.

## Structured state fields

| Field | Value |
|---|---|
| Foundation-State | `FOUNDATION_READY` |
| Ownership-State | `correct` |
| File-Cohesion-State | `COHESIVE` |
| Oversized/God-File-State | none (largest production file `BulkPurchaseInquiry.cs` = 116 LOC) |
| Localization-State | `CANONICAL` |
| API-Result-Pattern-State | `CANONICAL` |
| Stable-Error-Code-State | `CATALOGUED` |
| Logging-State | `CANONICAL` (no ad-hoc logging; no telemetry calls in module) |
| Sensitive-Logging-State | `NONE` |
| OpenTelemetry-State | `CANONICAL` |
| Correlation-Trace-State | `CANONICAL` |
| CQRS-State | `COMPLIANT` |
| Validator-Coverage-State | `EXHAUSTIVE` (1/1 endpoint-reachable request, validator present) |
| Contracts-Boundary-State | `CLEAN` |
| Cross-Module-Coupling-State | `LEGAL_CONTRACTS_ONLY` |
| Cross-Module-Join-State | `NONE` |
| Persistence-Ownership-State | `CORRECT` (own schema `bulk_inquiry`) |
| Endpoint-Ownership-State | `MODULE_OWNED` |
| Host-Residue-State | `ALLOWED_COMPOSITION_ROOT` only (4 Program.cs seam lines + module list entry) |
| Schema-Migration-State | `UNCHANGED` |
| Behavior-Preservation-Risk | `LOW` |
| Canonical-Reference-Used | Offer / Fulfillment / AccessControl (`Validation/` + `*ValidationCodes.cs`); BuildingBlocks (`Result`, `ApiResponseFactory`, `SemanticException`, `ContractOperationException`, `IErrorCatalogContributor`, `IErrorResourceSet`); ProductQnA (sibling Storefront write module) |
| Final-Disposition | `READY_TO_MIGRATE` |

## Responsibility map

| Responsibility | Owner |
|---|---|
| Storefront bulk-inquiry POST route | `Tooba.BulkInquiry.Endpoints.Storefront` |
| Submit CQRS command + handler | `Tooba.BulkInquiry.Application.Storefront.Commands` |
| Transport validator | `Tooba.BulkInquiry.Application.Storefront.Validators` |
| Typed-fault → `Result<T>` seam | `Tooba.BulkInquiry.Application.Composition` |
| Request/result models, directory port | `Tooba.BulkInquiry.Application.{Models,Ports}` |
| `BulkPurchaseInquiry` aggregate + status enum | `Tooba.BulkInquiry.Domain` |
| Directory / DbContext / Outbox / migrations | `Tooba.BulkInquiry.Infrastructure` |
| Stable error codes + catalog + resx | `Tooba.BulkInquiry.Contracts` |
| Host composition (DI + route map) | `Tooba.Host` — allowed |

## Ownership map

All responsibilities are in their true owning module. No `MUST_SPLIT` file. No Host business authority.

Host references (verified complete set):

- `Program.cs` — `using Tooba.BulkInquiry.Endpoints;` (L44), `AddBulkInquiryEndpointPresentation()` (L116), handler-assembly registration (L196), `MapBulkInquiryModuleEndpoints()` (L434) → `ALLOWED_COMPOSITION_ROOT`.
- `Composition/ToobaModuleComposition.cs` — `new BulkInquiryModule()` (L70) → `ALLOWED_COMPOSITION_ROOT`.
- `Tooba.MigrationRunner/ModuleMigrationRegistry.cs` — module schema descriptor → `ALLOWED_COMPOSITION_ROOT`.
- No Host BulkInquiry folder, no Host endpoint, no Host persistence, no Host business policy.

## Current illegal dependencies

**ZERO.** No foreign `*.Application` / `*.Infrastructure` / `*.Domain` reference from any BulkInquiry production file. The only cross-module seam is `Tooba.Catalog.Contracts.Ports.ICatalogReviewProductLookup` (Contracts-only, narrow DTO `CatalogReviewableProductDto`).

## Cross-module join inventory

**NONE.** `BulkInquiryDirectory` reads only its own `BulkInquiryDbContext` and the Catalog Contracts port; no EF navigation across modules, no cross-schema SQL.

## CQRS / MediatR gaps

**NONE.** Endpoint → `ISender` → `IRequest<Result<SubmitBulkInquiryResult>>` → `IRequestHandler<,>`; MediatR registered through `AddToobaCqrsFoundation`. No endpoint persistence/directory access.

## Validation classification matrix

| Request | Classification | Validator |
|---|---|---|
| `SubmitBulkInquiryCommand` | `VALIDATOR_REQUIRED` | `SubmitBulkInquiryCommandValidator` (present, auto-discovered) |

No other endpoint-reachable request exists.

## Localization findings

- Canonical mechanism present: `BulkInquiryErrorCodes` (Contracts), `BulkInquiryErrorCatalogContributor` (Contracts), `BulkInquiryErrorResourceSet` (Contracts), `BulkInquiryErrors.resx` + `.fa.resx` (Contracts).
- All 6 stable codes have descriptors **and** both-culture resources. No hard-coded Persian/English user-facing text. No `ex.Message` classification. No endpoint `Accept-Language` parsing.
- **Structural deviation (non-blocking):** validation codes live inside `Contracts.Errors.BulkInquiryErrorCodes`, whereas the repository's current AMSC sibling precedent keeps validator transport codes in `Application/Validation/<Module>ValidationCodes.cs` (AccessControl, Cart, Fulfillment, Settlement, Offer).

## API result / error mapping findings

- Endpoint returns `api.Created(location, result)`; no `Results.Json` / `Results.BadRequest` / local ProblemDetails builder / endpoint catch-map.
- Expected business failure → `SemanticException(SemanticError(code))` → `BulkInquiryOperation.ExecuteAsync` → `Result<T>` → `ApiResponseFactory`.

## Logging / sensitive-data findings

- No logging, no `Console.WriteLine`, no second telemetry pipeline, no secrets in the module.
- Request DTO carries `Phone` / `Email` / `Address`; none are logged or emitted.

## OpenTelemetry / correlation findings

- No custom correlation, no direct `StartActivity`, no manual `traceparent`. Presentation supplies `traceId`/`correlationId` through the canonical `IProblemDetailsContextProvider`.
- No cross-module call requiring `IModuleCallTracer` decoration (single Contracts read).

## File cohesion / splitting plan

`COHESIVE`. No file requires splitting. Two structural refinements only:

1. `Application/Storefront/Validators/` → `Application/Validation/` (technical-axis placement + validation-codes ownership precedent).
2. `Domain/Aggregates/BulkPurchaseInquiry.cs` → `Domain/BulkInquiry/` (module-established Domain capability sub-folder precedent).

## Exact target paths / namespaces

```text
Modules/BulkInquiry/
  Tooba.BulkInquiry.Contracts/       Errors/ + Resources/          (unchanged)
  Tooba.BulkInquiry.Domain/          BulkInquiry/{BulkPurchaseInquiry,Enums/BulkInquiryStatus}.cs
  Tooba.BulkInquiry.Application/     Composition/ + Models/ + Ports/ + Storefront/Commands/ + Validation/
  Tooba.BulkInquiry.Infrastructure/  BulkInquiryModule.cs + Directories/ + Persistence/(Migrations/)
  Tooba.BulkInquiry.Endpoints/       BulkInquiryEndpointModule.cs + Storefront/
```

Solution grouping `/Modules/BulkInquiry/` already canonical in `Tooba.slnx` (5 projects incl. Contracts + Endpoints).

## Behavior-preservation checklist

Route `POST /v1/storefront/products/{slug}/bulk-inquiries`; `201 Created` + `Location`; request/response DTO semantics; domain invariants (name/phone/quantity/address/email/company/notes limits); stable codes `bulk_inquiry.rejected` + 5 `bulk_inquiry.validation.*`; localization keys + both-culture text; `bulk_inquiry` schema, table, columns, index, migration IDs; DI lifetimes; outbox registration; Host composition seams.

## Certification blockers (ARCH-COMPLETE-002, current state)

1. **Legacy certification state.** `bulkInquiryAmc001` (AMC-001) records `structureState: READY_FOR_CERTIFY` and manifest `structureCertified: true`, but the module has never been certified as AMSC-001 under ARCH-COMPLETE-002.
2. **Domain fault typing.** Domain throws `SemanticException`/`SemanticError` and `Tooba.BulkInquiry.Domain` references `Tooba.BulkInquiry.Contracts`. The dominant AMSC-certified Domain convention (Order, Payment, Settlement, Inventory, Content, Fulfillment) is `ContractOperationException(code)` with **no** Domain→Contracts project reference.
3. **Validator-code ownership + placement.** Validation codes live in `Contracts.Errors` and validators under `Storefront/Validators/`; AMSC precedent is `Application/Validation/` + `<Module>ValidationCodes.cs`.
4. **SoT/manifest honesty.** No AMSC-001 SoT record, no Master Recovery checkpoint, no ARCH-COMPLETE-002 durable guard, no AMSC evidence tree.

## Wave plan

| Wave | Focus |
|---|---|
| W1 Migrate | Domain typed faults → `ContractOperationException`; Domain drops Contracts ref; validation codes → `Application/Validation/BulkInquiryValidationCodes.cs`; validators → `Application/Validation/`; typed catalog-absence rejection; guard updates |
| W2 Structure | Canonical `Application/Validation/` + `Domain/BulkInquiry/` foldering; solution grouping verification; structure guard + structure evidence |
| W3 Certify | ARCH-COMPLETE-002 certification; AMSC SoT record + manifest; Master Recovery checkpoint; durable cert guard; certification evidence |

## Structure-Handoff-State

`REQUIRED` — W2 owns the physical/visual gate; this analysis does not claim final structural readiness.

## Microservice extractability

Already strong: own schema, own migrations, zero foreign Application/Infrastructure/Domain coupling, Contracts-only Catalog seam, module-owned endpoints, Host composition-only residue. Remaining extraction work is zero-coupling-preserving quality closure (fault typing, validator-code ownership, AMSC certification state).
