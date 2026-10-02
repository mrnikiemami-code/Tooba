# TB-TMAR-MEDIA-AMC-001 — W0 Analyze

Mode: ANALYSIS_ONLY (Architect-direct AMSC)  
Target: `Tooba.Media.*`  
Skills: Analyze → Migrate → Structure → Certify  
Goal: COMPLETE_REFERENCE_PATTERN / ARCH-COMPLETE-002, microservice-extractable, ZERO foreign App/Infra/Domain coupling

## Current physical state

| Project | Observation |
| --- | --- |
| Application | ROOT DUMP: single `MediaContracts.cs` — DTOs + `IMediaDirectory` + `IMediaObjectStore`; **no CQRS/MediatR/validators** |
| Domain | Root `MediaAsset.cs` mixes enum + aggregate (should be Aggregates/ + Enums/) |
| Contracts | Partial folders (`Assets/`, `Ports/`); **no Errors catalog / resx** |
| Infrastructure | Root dumps `MediaDirectory.cs`, `LocalFileMediaStore.cs`, `MediaModule.cs`; Migrations at root (not under Persistence/); capability folders partial (`Assets/`, `Adapters/`, `Persistence/`) |
| Endpoints | **PRESENT** — Admin + Storefront + binary serve; calls Application ports directly (no `ISender`) |
| Solution Explorer | Media projects under flat `/Modules/` — **not** `/Modules/Media/` |
| Manifest | Media **absent** from `tmar-module-structure-manifests.json` |

## Host boundary

- Host already maps `MapMediaModuleEndpoints()`; composition via `MediaModule` + Dev `MediaDbContext` migrate seam.
- Host Media HTTP evacuated (guard: `HostMediaEvacuationGuardTests`).
- Host → Media.Application/Domain in business Host folders: **ZERO** (composition/dev only).

## Coupling scan

- Media → foreign App/Infra/Domain: **ZERO** (csproj + usings)
- Cross-module EF joins from Media: **NONE** (own `MediaDbContext`)
- Content owns article-gallery media binding via Content.Application (out of Media AMSC scope)

## HTTP / canonical mechanisms (blockers)

- Endpoints use ad-hoc `Results.Json` + hard-coded Persian titles + `PlatformHttpException` catch — **not** `ApiResponseFactory` / `Result` / catalog localization
- Infrastructure throws `PlatformHttpException` with user-facing Persian strings (parallel error pipeline)
- Stable codes exist ad-hoc (`media.upload.failed`, `media.missing`, …) without `IErrorCatalogContributor` / `.resx`

## Structure-Handoff-State

**REQUIRED** — Solution Explorer grouping + Application/Domain/Infra foldering + CQRS/Result/validators + error catalog before Certify.

## Blockers for COMPLETE_REFERENCE_PATTERN

1. `SOLUTION_EXPLORER_MISSING_GROUPING` — no `/Modules/Media/`
2. `APPLICATION_NOT_CQRS` — no Commands/Queries/MediatR
3. `APPLICATION_ROOT_DUMP` — `MediaContracts.cs`
4. `ENDPOINTS_BYPASS_CQRS_RESULT` — direct directory calls + ad-hoc Problem JSON
5. `ERROR_CATALOG_MISSING` — no Media error contributor/resources
6. `DOMAIN_ROOT_MIXED` — enum+aggregate in one root file
7. `INFRA_ROOT_DUMP` / Migrations placement
8. `MANIFEST_ABSENT`

## Capability map (business axes)

| Capability | Contents |
| --- | --- |
| Assets | upload/query/get metadata (`IMediaDirectory`) |
| Storage | object store (`IMediaObjectStore` / local files) |
| Serving | binary HTTP serve (admin public + storefront) |
| Readiness | Contracts readiness/demo ports + Infra bridges |

## Wave plan (Architect-direct)

| Wave | Mode | Scope | Commit |
| --- | --- | --- | --- |
| W0 | Analyze | this evidence | docs |
| W1 | Migrate | `.slnx` `/Modules/Media/` | impl |
| W2 | Migrate+Structure | Domain Aggregates/Enums; Application Ports/Models; Infra Storage/Persistence; Contracts Errors shell | impl |
| W3 | Migrate | CQRS + FluentValidation + Result/`api.From` + error catalog; thin Endpoints | impl |
| W4 | Structure+Certify | structure gate, durable guards, manifest, SoT | cert |

Host final closure preserved. Frontend frozen. No schema/migration content changes (path moves of migration files only if required for structure; prefer keep Migrations under Persistence/).

## Microservice extractability target

After W4: Media owns persistence + binary store + business + HTTP; Host only composition/dev migrate; Contracts-only inbound/outbound.
