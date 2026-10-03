# TB-TMAR-LOCALIZATION-AMC-001 — W0 Analyze

Mode: ANALYSIS_ONLY (Architect-direct AMSC)  
Target: `Tooba.Localization.*`  
Skills: Analyze → Migrate → Structure → Certify  
Goal: COMPLETE_REFERENCE_PATTERN / ARCH-COMPLETE-002, microservice-extractable, ZERO foreign App/Infra/Domain coupling

## Current physical state

| Project | Observation |
| --- | --- |
| Application | ROOT DUMP: single `LanguageContracts.cs` — snapshots + directory DTO “commands” + `ILanguageDirectory` + mappings; **no MediatR IRequest/handlers/validators** |
| Domain | Root `Language.cs` mixes enums + aggregate |
| Contracts | Root ports + `Errors/LanguageErrorCodes.cs`; **no resx / IErrorResourceSet**; catalog lives in Endpoints |
| Infrastructure | Root dumps (`LanguageDirectory`, bridges, bootstrap, `LocalizationModule`, Outbox); Persistence/Migrations OK |
| Endpoints | PRESENT Admin languages; calls `ILanguageDirectory` directly; `Results.Json` success; try/catch + `api.From*Exception` |
| Solution Explorer | Localization projects under flat `/Modules/` — **not** `/Modules/Localization/`; **Endpoints project missing from slnx** |
| Manifest | Localization **absent** from `tmar-module-structure-manifests.json` |
| structureLock | Localization **omitted** from `certifiedModules` |

## Host boundary

- Host maps `MapLocalizationModuleEndpoints()` + `AddLocalizationEndpointPresentation()`
- Host retains thin `HostLocalizationAdminAuthorizer` (platform seam — KEEP)
- Composition via `LocalizationModule` + schema migrator

## Coupling scan

- Localization → foreign App/Infra/Domain: **ZERO** (csproj + usings)
- Cross-module EF joins from Localization: **NONE** (own `LocalizationDbContext`)
- Outbound Contracts ports: `ILanguageLookup`, `ILanguageActivationPort`, `ILanguageReferenceGuard` (legal)

## HTTP / canonical mechanisms (blockers)

- Endpoints bypass MediatR/`ISender`; success uses ad-hoc `Results.Json`
- Error catalog contributor under Endpoints (should be Contracts + resx + Infra/Endpoints registration)
- No FluentValidation inventory for endpoint-reachable requests
- Application “CreateLanguageCommand” names collide with CQRS naming but are directory DTOs (not `IRequest`)

## Structure-Handoff-State

**REQUIRED** — Solution Explorer `/Modules/Localization/` + Endpoints in slnx + Application/Domain/Infra foldering + CQRS/Result/validators + Contracts error resources before Certify.

## Blockers for COMPLETE_REFERENCE_PATTERN

1. `SOLUTION_EXPLORER_MISSING_GROUPING` — no `/Modules/Localization/`
2. `ENDPOINTS_MISSING_FROM_SLNX`
3. `APPLICATION_NOT_CQRS` — no MediatR handlers
4. `APPLICATION_ROOT_DUMP` — `LanguageContracts.cs`
5. `ENDPOINTS_BYPASS_CQRS_RESULT` — direct directory + `Results.Json`
6. `ERROR_CATALOG_MISPLACED` / missing resx + `IErrorResourceSet`
7. `DOMAIN_ROOT_MIXED` — enums+aggregate
8. `INFRA_ROOT_DUMP` / Outbox at root
9. `MANIFEST_ABSENT`

## Capability map

| Capability | Contents |
| --- | --- |
| Languages | Admin CRUD/list + directory persistence |
| Lookup/Activation | Contracts ports + Infra bridges |
| Bootstrap | hosted language seed |

## Wave plan (Architect-direct)

| Wave | Mode | Scope | Commit |
| --- | --- | --- | --- |
| W0 | Analyze | this evidence | docs |
| W1 | Migrate | `.slnx` `/Modules/Localization/` + Endpoints project | impl |
| W2 | Migrate+Structure | Domain Aggregates/Enums; Application Ports/Models; Infra folders; Contracts Errors+Resources shell | impl |
| W3 | Migrate | CQRS + FluentValidation + Result/`api.From` + error catalog wiring; thin Endpoints | impl |
| W4 | Structure+Certify | structure gate, durable guards, manifest, SoT, structureLock | cert |

Host final closure preserved (authorizer seam). Frontend frozen. No schema/migration content changes.

## Microservice extractability target

After W4: Localization owns persistence + language registry business + HTTP; Host only composition/dev migrate + admin authorizer adapter; Contracts-only inbound/outbound.
