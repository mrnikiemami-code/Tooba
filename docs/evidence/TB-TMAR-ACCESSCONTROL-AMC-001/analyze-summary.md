# AccessControl module AMC Analyze — TB-TMAR-ACCESSCONTROL-AMC-001

Target: `Tooba.AccessControl.*`
Skills: analyze → migrate → certify (COMPLETE_REFERENCE_PATTERN)
Mode: ANALYSIS_ONLY

## Prior claim vs current skill reality

Historical SoT `accessControlArchComplete002Structure` claimed CERTIFIED. Against **current** AMC/Complete skill (and Story-hardened bar), AccessControl is **`FOUNDATION_PARTIAL` / NOT_CERTIFIED_FOR_REENFORCEMENT**.

## Blockers (must migrate)

| ID | Finding | Severity |
| --- | --- | --- |
| B1 | **VS Solution**: projects live under flat `/Modules/` — no `/Modules/AccessControl/`; **Endpoints project absent from `Tooba.slnx`** | STRUCTURE |
| B2 | **Ad-hoc HTTP errors**: `Results.Json({title,code})` + `ace.Code.Contains("escalation"|"ceiling")` status heuristic | API / localization |
| B3 | **Hardcoded FA user text** in `AccessControlException` messages, `PermissionCatalog`, `AccessControlCapabilityGate` PlatformHttpException messages | LOCALIZATION |
| B4 | **Handlers mostly return raw DTO/`Unit`**, not `Result`/`Result<T>`; endpoints catch exceptions instead of `api.From` | RESULT_PIPELINE |
| B5 | **Endpoints import Domain** (`AccessOwnerScopeKind`, etc.) — Endpoints→Domain leakage | BOUNDARY |
| B6 | **Application `Models/AccessControlContracts.cs`** mixed DTO + exception + port dump (forbidden `*Contracts.cs` Application bundle) | COHESION |
| B7 | **Domain root god file** `AccessControlDomain.cs` (enums + entities) — not Aggregates/Enums foldering | FOLDERING |
| B8 | No module `Errors` catalog contributor / `.resx` for AccessControl machine codes | LOCALIZATION |

## Already good / preserve

- Host AccessControl residue ZERO (historical)
- Infra ProjectReferences to foreign modules are **Contracts-only** (Catalog/Identity/Party) — legal for microservice
- Application refs Identity/OperatorProfile/Catalog **Contracts-only**
- Validators exist for 6 write commands; Persistence migrations under `Persistence/Migrations`
- CQRS MediatR handlers present; capability gate in Application

## Recommended waves

1. **W1** — Solution folder `/Modules/AccessControl/` + add Endpoints project to slnx
2. **W2** — Semantic/localization failure channel (stable codes + catalog/resx; zero FA exception text; zero Code.Contains HTTP mapping)
3. **W3** — `Result<T>` + `ApiResponseFactory.From` / `Created` on all endpoint-reachable requests
4. **W4** — Capability/structure polish (Domain Aggregates/Enums; rename Models dump; Endpoints Domain ZERO)
5. **W5-CERT** — ARCH-COMPLETE-002 re-certify + durable guards + honest SoT/manifest

## Microservice goal

Zero foreign Application/Infrastructure/Domain coupling (already Contracts-only). Harden presentation/Result so the module extracts without Host error-mapping choreography.

## Behavior

No production code change in Analyze wave.
