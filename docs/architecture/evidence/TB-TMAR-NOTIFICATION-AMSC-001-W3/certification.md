# TB-TMAR-NOTIFICATION-AMSC-001-W3 — Certification evidence (tooba-architecture-certify)

- Starting HEAD: `e2f23975` (W2), `HEAD == origin/main`
- Skill: `tooba-architecture-certify`
- Target: `src/backend/Modules/Notification/Tooba.Notification.*`
- Verdict: **COMPLETE_REFERENCE_PATTERN / ARCH-COMPLETE-002 STRUCTURE_CERTIFIED**
- Zero production change in this wave (SoT + manifest + durable guard + evidence only).

## 1. Structure gate (handoff from W2)

`Structure-State = READY_FOR_CERTIFY` accepted with current evidence
(`TB-TMAR-NOTIFICATION-AMSC-001-W2/structure.md`): `PROFESSIONAL_SHALLOW`, `CANONICAL` solution
grouping, `EXACT` path↔namespace, `CLEAN` physical copies, `ENFORCED` root allowlists. Certify
re-checked the invariants as defense in depth via the new durable guard
(`NotificationModuleAmsc001W3CertGuardTests`) — all green.

## 2. Final physical tree

```text
Contracts/      Commands/ Copy/ Dtos/ Errors/ Ports/ Resources/ Routes/
Domain/         Aggregates/ ValueObjects/
Application/    Customer/{Commands,Queries}/<UseCase>/  Seller/{Commands,Queries}/<UseCase>/
                Composition/ Models/ Ports/ Rendering/ Validators/
Infrastructure/ DependencyInjection/ Directories/ Handlers/ Messaging/ Observability/ Projectors/ Persistence/Migrations/
Endpoints/      Customer/ Seller/ Errors/ + NotificationEndpointModule.cs (root composition entry)
```

## 3. Canonical mechanism verification

| Concern | Mechanism | Proof |
| --- | --- | --- |
| API result/error | `ApiResponseFactory.From` / `FromFailure` on all 10 endpoints | guard asserts 5 `sender.Send(` + factory per surface; zero `Results.Json/BadRequest/Problem`, zero `ex.Message` |
| Typed faults | `ContractOperationException` + `NotificationOperation` seam (`IsKnown` filter) | W3 guard + W1 guard |
| Stable codes | `Contracts/Errors/NotificationErrorCodes` (5 declared, `IsKnown`) | W1 guard literal-zero check |
| Localization | `NotificationErrorResourceSet` (`notification.` keyspace) + bilingual resx + foundation localizer | W1 guard key-pair check |
| Catalog | 5 single-owner descriptors; `customer.session.required` Foundation-owned, consumed not re-registered | W1 guard |
| Transport validation | 6 FluentValidation validators → `notification.validation.*` → foundation `validation.failed` | W1 guard (paths updated in W2) |
| Logging/telemetry | counters-only `NotificationInstrumentation`; no logger misuse; no second pipeline | W0 audit retained |
| Correlation/trace | none of `StartActivity`/custom header/`AsyncLocal`/`traceparent` parsing (module issues no cross-module calls) | W0 audit retained |
| CQRS | MediatR 12.5.0 via foundation; 10 requests, `ISender` dispatch | module + Host guards |

## 4. Request → handler → validator matrix (10/10 classified)

| Request | Validator | Class |
| --- | --- | --- |
| `ListCustomerNotificationsQuery` | `ListCustomerNotificationsQueryValidator` | REQUIRED ✓ |
| `GetCustomerUnreadNotificationCountQuery` | — | NO_VALIDATOR_REQUIRED (auth-scoped) |
| `MarkCustomerNotificationReadCommand` | `...Validator` | REQUIRED ✓ |
| `MarkAllCustomerNotificationsReadCommand` | — | NO_VALIDATOR_REQUIRED (auth-scoped) |
| `DismissCustomerNotificationCommand` | `...Validator` | REQUIRED ✓ |
| `ListSellerNotificationsQuery` | `...Validator` | REQUIRED ✓ |
| `GetSellerUnreadNotificationCountQuery` | — | NO_VALIDATOR_REQUIRED (auth-scoped) |
| `MarkSellerNotificationReadCommand` | `...Validator` | REQUIRED ✓ |
| `MarkAllSellerNotificationsReadCommand` | — | NO_VALIDATOR_REQUIRED (auth-scoped) |
| `DismissSellerNotificationCommand` | `...Validator` | REQUIRED ✓ |

`validatorCoverageState = EXHAUSTIVE` (6 required present, 4 not-required with durable reasons,
gap ZERO).

## 5. Boundary / persistence / Host audits

- Foreign coupling: **ZERO** foreign `*.Application` / `*.Infrastructure` / `*.Domain` project
  references in any Notification project (W3 guard). Foreign edges are Contracts-only:
  `Order.Contracts` (`IOrderNotificationReader` + Storefront/customer context contracts),
  `Payment/Fulfillment/Returns.Contracts` (integration events).
- Consumers of Notification reach only `INotificationCreationPort` / `NotificationSemanticTypes`
  in Contracts (Wallet, Support).
- Cross-module join: **NONE** — `NotificationDbContext` owns only the `notification` schema.
- Schema: migration `20260827111240_InitialNotification` + snapshot untouched
  (`migrationFilesChanged = 0`); no regeneration.
- Host residue: `Program.cs` composition registrations (assembly scan, endpoint map, presentation,
  seller-authorizer DI), `Security/Seller/HostNotificationSellerAuthorizer.cs` (thin security
  adapter over the module-declared seam), `ModuleMigrationRegistry.cs` descriptor. All
  `ALLOWED_COMPOSITION_ROOT` / `ALLOWED_SECURITY_ADAPTER`. No `Host/Notifications` folder; no
  Host-growth regression (closed-folder audit: zero files added to any Host folder).
- Frontend: untouched (frozen).

## 6. Focused validation

| Run | Result |
| --- | --- |
| `dotnet build Tooba.Notification.Tests` (all 5 module projects) | 0 errors |
| `dotnet build Tooba.Host` | 0 errors |
| `dotnet test Tooba.Notification.Tests` | **18 passed / 0 failed** |
| `dotnet test --filter NotificationModuleAmsc001W1` | **11 passed / 0 failed** |
| `dotnet test --filter NotificationModuleAmsc001W3` | **9 passed / 0 failed** (new durable guard) |

Pre-existing unrelated red (unchanged, identical to the Media AMSC W3 disclosure, not repaired —
module-local scope): `TmarDurableGuardTests.Recovery_*` (frozen 16-module list),
`TmarCompleteReferenceStructureGateTests.Certified_modules_satisfy...` (Catalog Contracts
namespace debt). No guard weakened; no open-ended repair loop.

## 7. Manifest promotion + SoT

- `tmar-module-structure-manifests.json`: Notification promoted from `uncertifiedHttpOwningModules`
  to `modules[]` — `structureCertified: true`, `lockVersion: ARCH-COMPLETE-002`, per-project root
  allowlists, forbidden root files, forbidden top-level folders. Exactly one certified entry.
- `tmar-current-state.json`: `notificationModuleAmsc001W3` block recorded (this file),
  `structureLock.certifiedModules` extended with `Notification`; W0/W1/W2 lineage blocks retained.
- `TOOBA-TMAR-MASTER-RECOVERY.md`: AMSC-001 Notification checkpoint appended (current region).
- Master Recovery stale-pointer audit: no historical task presented as next; the Media lineage
  SHAs remain recorded; no other module's blocks were rewritten.

## 8. Touched-surface certification

Every production file touched by W1/W2 was re-read and verified: cohesive single responsibility;
correct capability folder; exact path↔namespace; no root dump; no obsolete/duplicate type; no
hard-coded user-facing localized text (fault titles come from the catalog/resx); no foreign
Application/Infrastructure/Domain leakage; no parallel canonical mechanism; no unintended
behavior/schema change (18/18 behavior tests green across all waves).

## 9. Residual non-blocking debt

None attributable to Notification. Accepted, explicit, documented non-conformances retained (not
debt) — Domain aggregate literal invariants, outbox framework invariant, read-time copy rendering,
client-driven `locale` parameter — see the SoT block `acceptedRetainedNonConformances`.

## 10. Verdict

```text
COMPLETE_REFERENCE_PATTERN
ARCH-COMPLETE-002 STRUCTURE_CERTIFIED — Notification
Stop gate: USER_REVIEW_NOTIFICATION_AMSC_001_W3
automaticNextImplementationTask = NONE
```
