# TB-TMAR-USERPREFERENCE-AMC-001-W0 — Analyze

## Mode

`ARCHITECT_DIRECT_AMSC` — Analyze only.

## Ownership

| Surface | Owner |
|---|---|
| Customer locale HTTP + Admin locale/UI preference HTTP | UserPreference.Endpoints |
| CQRS LocalePreferences / UiPreferences | UserPreference.Application |
| UserPreference + UiPreference aggregates | UserPreference.Domain |
| Directories / seed / DbContext | UserPreference.Infrastructure |
| Stable error codes | UserPreference.Contracts.Errors |
| Host Preferences HTTP | CLOSED_HOST_ZERO (module endpoints) |

## Foreign coupling

- Foreign Application/Infrastructure/Domain: **ZERO**
- Cross-module Contracts: `Order.Contracts` (actor/session seam in Endpoints)
- Self-contained persistence schema `user_preference` (module-owned)

## Blockers for COMPLETE_REFERENCE_PATTERN

1. **Solution Explorer:** Domain/Application/Infrastructure sit under flat `/Modules/`; Contracts + Endpoints are **absent from `Tooba.slnx`**; need `/Modules/UserPreference/` with all 5 projects.
2. **Structure:**
   - Domain root dumps `UserPreference.cs` / `UiPreference.cs` (need `Aggregates/`)
   - Application root dumps `UserPreferenceContracts.cs` (ports+models) + `UserPreferenceShapes.cs`
   - Validators co-located in Command files with ad-hoc string error codes
   - Infrastructure root `*Directory.cs`; Outbox nested in `UserPreferenceModule.cs`
3. **Error ownership:** Catalog + resx in Endpoints; validation codes hard-coded strings; move catalog/resx to Contracts; register in Infrastructure.
4. **API result:** Endpoints use `Results.Json` + `catch (SemanticException)` — need `IRequest<Result>` + `UserPreferenceOperation` + `ApiResponseFactory.From`.
5. **Validator inventory:** codes not in Contracts catalog; Get queries Actor-only need classification.
6. **Structure-Handoff-State:** `REQUIRED`

## Wave plan

| Wave | Focus |
|---|---|
| W1 | `/Modules/UserPreference/` slnx grouping (5 projects) |
| W2 | Domain/App/Infra/Contracts capability-first physical tree |
| W3 | Contracts catalog/resx + Result/Operation + thin endpoints |
| W4 | Structure + Certify + SoT/manifest |

## Microservice extractability

Blocked until Solution Explorer, structure, Result/API mapping, and Contracts-owned catalog close. Foreign coupling already Contracts-only — extractable once quality gates pass.
