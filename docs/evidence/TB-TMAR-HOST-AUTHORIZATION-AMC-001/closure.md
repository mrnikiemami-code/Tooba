# TB-TMAR-HOST-AUTHORIZATION-AMC-001 — Certification / Closure

Skills: `tooba-architecture-analyze` → `tooba-architecture-migrate` → `tooba-architecture-certify`
Transport: `ARCHITECT_DIRECT_ANALYZE_MIGRATE_CERTIFY` (user-requested direct run, no Bridge claim)
Parent: `TB-TMAR-HOST-REMAINDER-AUDIT-001` (the audit that ranked this as the next Host evacuation)
Parent commit: `3ad429dc` (`TB-TMAR-HOST-MEDIA-EVACUATE-001`)

## 1. Verdict of the certified slice

The **Host `Authorization` capacity** is evacuated and its destination surface is internally
canonical: `CERTIFIED_SLICE`.

The **AccessControl module** was already `COMPLETE_REFERENCE_PATTERN` /
`ARCH-COMPLETE-002 STRUCTURE_CERTIFIED` (see
`docs/evidence/TB-TMAR-ACCESSCONTROL-FINAL-CERTIFICATION-AND-SOT-CLOSURE-001/accesscontrol-final-certification.md`)
and remains so; this task did **not** promote a new module certification. There is nothing for a
new module-level ARCH-COMPLETE-002 entry to claim, so
`tmar-module-structure-manifests.json` is deliberately **unchanged** (no duplicate certified
AccessControl entry; exactly one certified entry still exists).

## 2. Host ZERO proof

- `src/backend/Host/Tooba.Host/Authorization/` — **ABSENT** (`Test-Path` = `False`).
- `src/backend/Host/Tooba.Host/Health/SpiceDbHealthProbe.cs` — **ABSENT**.
- No Host source contains `Authzed.Api.V1` or `SpiceDbAuthorizationAdapter`.
- `Tooba.Host.csproj` contains **no** `Authzed.Net` package reference.
- Host-owned authorization routes = **ZERO** (the slice never owned any route).
- Host-owned authorization persistence = **ZERO**.

## 3. Destination ownership proof

`src/backend/Modules/AccessControl/Tooba.AccessControl.Infrastructure/Authorization/` — exactly 7 files:

| File | LOC |
| --- | --- |
| `AuthorizationAdapters.cs` | 193 |
| `AuthorizationInstrumentation.cs` | 60 |
| `AuthorizationRegistration.cs` | 67 |
| `SpiceDbAuthorizationAdapter.cs` | 344 |
| `SpiceDbAuthorizationBootstrapper.cs` | 170 |
| `SpiceDbAuthorizationOptions.cs` | 119 |
| `authorization-foundation.zed` | 23 |

## 4. Path ↔ namespace proof

Every production `.cs` in the slice declares `namespace Tooba.AccessControl.Infrastructure.Authorization;`
and lives directly under `…/Tooba.AccessControl.Infrastructure/Authorization/`, so
path-derived namespace equality is exact.

Path↔namespace = **EXACT**. Namespace-alias workaround = **NONE**. `TypeForwardedTo` shim = **NONE**.
Stale/duplicate physical copy of any moved type in Host = **NONE**.

## 5. Root allowlist

No new root file was created. `Tooba.AccessControl.Infrastructure` root remains exactly
`AccessControlModule.cs`, matching the existing certified manifest root allowlist. No manifest edit
was required or made.

## 6. File cohesion

Slice maximum is `SpiceDbAuthorizationAdapter.cs` at 344 LOC, below the 800 LOC new-file threshold and
below the certified `AccessControlDirectory` legacy ceiling. `ConfiguredAuthorizationSchemaBootstrapper`
was folded into `SpiceDbAuthorizationBootstrapper.cs` together with `SpiceDbHealthProbe` — both are
the same "apply/inspect bootstrap state" responsibility, so this is a genuine cohesion merge, not a
size-guard game. No god-file; no artificial parallel decomposition.

## 7. Endpoint ownership / CQRS

- Endpoint count owned by this slice = **0**; route parity trivially preserved.
- No MediatR request was added, removed or redirected by this slice.
- AccessControl's certified 19-request / 6-validator matrix is untouched.

## 8. Canonical mechanism compliance (touched surface)

| Concern | State |
| --- | --- |
| Failed-failure classification | **CANONICAL** — typed `SpiceDbUnavailableException`; no `ex.Message ==` sentinel |
| Options binding | **CANONICAL** — `IOptions<T>` + `IValidateOptions<T>` + `ValidateOnStart()`, bound in module registration |
| Logging | `ILogger<T>` structured only; no credential/token material logged (bootstrap log explicitly states "Token is not logged") |
| Telemetry | canonical `AuthorizationInstrumentation` metric surface; no second pipeline |
| Localization | **N/A** — this slice owns no user-facing message; no new user-facing text added |
| API result mapping | **N/A** — slice owns no HTTP response |
| Host composition consumption | `ALLOWED_COMPOSITION_ROOT` / `ALLOWED_SECURITY_ADAPTER` (readiness + options), not business authority |

Blocking categories — `RAW_RESULTS`, `AD_HOC`, `PARALLEL_MAPPER`, `UNREGISTERED_CODES`,
`DUPLICATE_ERROR_DESCRIPTOR`, `HARDCODED_TEXT`, `DUPLICATE_TELEMETRY`, `SECOND_PIPELINE`,
`PARALLEL_CORRELATION`, `LOST_PROPAGATION`, `ILLEGAL`, `FOREIGN_ACCESS`, `VIOLATION` — all **not
applicable / ZERO** for this slice.

## 9. Cross-module boundary

| From | To foreign Application/Infrastructure/Domain | Verdict |
| --- | --- | --- |
| `Tooba.AccessControl.Infrastructure.Authorization` | ZERO | CLEAN |
| `Tooba.AccessControl.Infrastructure.Authorization` → `Tooba.Host` | ZERO | CLEAN |
| `Tooba.Host` → `Tooba.AccessControl.Infrastructure.Authorization` | options type + probe resolve only | ALLOWED_COMPOSITION |

No cross-module DbContext, DbSet, join or shared mutable aggregate was introduced.

## 10. Binding correction (honest note)

Host readiness must report whether authorization is configured. Passing the whole
`SpiceDbAuthorizationOptions` object to the readiness evaluator is a legitimate platform boundary
(readiness is a Host platform concern) but leaks more of the module option model than necessary.
A narrower readiness contract (e.g. an `IAuthorizationReadiness` port) is recorded as
**non-blocking residual debt**, not a certification blocker: no manual `new`-based composition, no
service-locator business access, no foreign Application/Infrastructure/Domain dependency is involved.
It was not changed here to keep the migration behavior-preserving and schema/route-safe.

## 11. Persistence / migration safety

No migration file was added, removed, reordered or regenerated. No schema, table, column, index or
constraint changed. `ARCH-DATA-001` intact.

## 12. Durable guard

`Architecture/HostAuthorizationEvacuationGuardTests` (7 facts) locks the evacuation, the exact
destination slice, the SDK boundary, the composition seam, configuration/mode preservation, the
schema/fail-closed contract, and the typed-fault requirement. `HostAdminCanon007GuardTests` paths were
repointed without weakening any assertion.

## 13. Focused validation

- `dotnet build Host/Tooba.Host/Tooba.Host.csproj` → PASS (0/0)
- `dotnet build Host/Tooba.Host.Tests/Tooba.Host.Tests.csproj` → PASS (0 errors)
- Guard/behavior run → **Passed 32, Failed 0**
- Consumer/behavior run → **Passed 20, Skipped 8, Failed 0**

Pre-existing repository-wide `TmarSourceSizeAndInfraAppTests` debt is red at clean HEAD and is
explicitly out of scope; no violation entry references the evacuated slice. See `validation.md`.

## 14. Residual non-blocking debt

- Narrower Host readiness contract for authorization (see §10).
- Repository-wide stale baselines/inventory recorded by earlier tasks (not repaired here).
- Other Host folders (`Admin`, `Authentication`, `Seller`, `Storefront`, `Development`, `Grid`,
  `Composition`, root-level policies) remain deferred; none was started by this task.

## 15. Protected state

- Frontend = **FROZEN**
- Checkout = `PAUSED_AT_SAFE_W5_CHECKPOINT`
- `currentHostEvacuation.activeModule` remains `AddressBook`; the AddressBook user-review stop is
  untouched by this task.
- No schema/migration change, no route change, no AccessControl business-behavior change.

`workflowStop = USER_REVIEW_HOST_AUTHORIZATION_AMC_001`
