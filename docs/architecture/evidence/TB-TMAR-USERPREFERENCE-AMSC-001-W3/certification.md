# TB-TMAR-USERPREFERENCE-AMSC-001-W3 — Certify (`tooba-architecture-certify`)

```text
TASK:          TB-TMAR-USERPREFERENCE-AMSC-001-W3
MODE:          ARCHITECT_DIRECT_AMSC
SKILL:         tooba-architecture-certify
TARGET:        src/backend/Modules/UserPreference/Tooba.UserPreference.*
PARENT:        TB-TMAR-USERPREFERENCE-AMSC-001-W2
PARENT COMMIT: 2547037e
STARTING HEAD: 2547037e79cb2154872c0bfef036ee812c4b037b
STATE:         USERPREFERENCE_AMSC_001_CERTIFIED
VERDICT:       COMPLETE_REFERENCE_PATTERN
STRUCTURE:     STRUCTURE_CERTIFIED
LOCK VERSION:  ARCH-COMPLETE-002
STOP GATE:     USER_REVIEW_USERPREFERENCE_AMSC_001_W3
```

Final objective stated up front: **UserPreference must be extractable as an independent
microservice**, so the certification gates are *zero invalid coupling* and an *exact
path↔namespace* boundary. UserPreference already carried an ARCH-COMPLETE-002 certification
(`TB-TMAR-USERPREFERENCE-AMC-001-W4`); the AMSC-001 lineage re-certifies the same single `modules[]`
entry in place with the W0→W3 lineage. No pre-cert duplicate was created and no prior guard was
weakened.

---

## 1. `HTTP_OWNING` applicability gate (certify §0b)

UserPreference owns **real, frontend-consumed** HTTP routes, so the endpoint/CQRS/validator gates
apply in full and are asserted rather than excused.

| | Value |
| --- | --- |
| Module-owned route groups | **3** |
| Module-owned routes | **6** |
| Host-owned UserPreference routes | **0** |
| Endpoint-reachable requests | **4** |
| Endpoint ownership state | `MODULE_OWNED` |

Route groups and templates:

```text
/v1/customer/preferences          GET /   PUT /
/v1/admin/operator/preferences    GET /   PUT /
/v1/admin/ui-preferences          GET /{key}   PUT /{key}
```

`Host/Tooba.Host/Program.cs` maps the module boundary only
(`MapUserPreferenceModuleEndpoints`) and declares zero UserPreference route templates.
`Host/Preferences` remains `CLOSED_HOST_ZERO`.

## 2. CQRS

**COMPLIANT 4/4.** Every endpoint-reachable request is a real `IRequest<Result<...>>` with a real
`IRequestHandler<,>`, dispatched from a thin endpoint through `ISender`:

| Request | Kind | Validator |
| --- | --- | --- |
| `UpsertUserPreferenceCommand(Guid ActorUserId, string Locale)` | Command | `UpsertUserPreferenceCommandValidator` |
| `GetUserPreferenceQuery(Guid ActorUserId)` | Query | — (server-derived actor) |
| `UpsertUiPreferenceCommand(Guid ActorUserId, string Key, string JsonPayload)` | Command | `UpsertUiPreferenceCommandValidator` |
| `GetUiPreferenceQuery(Guid ActorUserId, string Key)` | Query | `GetUiPreferenceQueryValidator` |

No endpoint business logic, no direct Directory/DbContext call from an endpoint, no custom
dispatcher, no Host bypass. MediatR registration goes through `AddToobaCqrsFoundation` for the
`Tooba.UserPreference.Application` assembly.

## 3. Validator matrix — `EXHAUSTIVE 4/4`

Set equality holds between the reachable request set and the classified set:
**3 `VALIDATOR_REQUIRED`** (validators present) + **1 `NO_VALIDATOR_REQUIRED`**
(`GetUserPreferenceQuery` has no caller-controlled transport shape — its only input is the
server-derived actor).

## 4. Stable codes, descriptors and localization

| | Value |
| --- | --- |
| Declared string constants (incl. Foundation-owned `SessionRequired`) | 11 |
| UserPreference-owned declared codes (`KnownCodes`) | **10** |
| Registered descriptors | **10** |
| EN resource keys | **10** |
| FA resource keys | **10** |
| Raw prose faults | **0** |

- Single canonical home: `Tooba.UserPreference.Contracts/Errors/UserPreferenceErrorCodes.cs`
  (`Tooba.UserPreference.Contracts.Errors`) with the declared-code guard
  (`private static readonly HashSet<string> KnownCodes` + `public static bool IsKnown(string?)`).
- `customer.session.required` stays **Foundation-owned**: the constant is consumable at the two
  customer endpoint `api.FromFailure` sites but `IsKnown("customer.session.required") == false`, and
  its descriptor is never claimed. Duplicate usage allowed, duplicate descriptor ownership not.
- One descriptor per module code, each `LocalizationKey == Code`, all resolving through the composed
  `ErrorDefinitionCatalog` with a unique owner (repo-global
  `ErrorCatalogUniqueCodeGuardTests` stays green).
- Bilingual resources resolve through the canonical composed `ResourceErrorMessageLocalizer` with
  **real Persian text verified per code**, and the explicit
  `EmbeddedResource`/`LogicalName` pair is pinned so the contract is locked rather than implicit.
- Registration happens **exactly once** by the Infrastructure composition root
  (`UserPreferenceModule.AddServices`).

## 5. Typed-fault seam

`UserPreferenceOperation` classifies only by typed code — `catch (ContractOperationException ex) when
(UserPreferenceErrorCodes.IsKnown(ex.Code))` plus `catch (SemanticException ex)` — in both the value
and value-less overloads. Zero `ex.Message`, zero `StartsWith`, zero `IResult`, zero
`InvalidOperationException("<literal>")`, zero `Results.Json`/`Results.BadRequest`/`Results.Problem`
and zero local `ProblemDetails` builder anywhere in the production surface.

## 6. Contracts-only microservice boundary

Every project edge is own-module layering or the `Tooba.BuildingBlocks` / `Tooba.ModuleContracts` /
`Tooba.Persistence` foundations, with exactly **one** legal foreign edge:
`Tooba.UserPreference.Endpoints -> Tooba.Order.Contracts` (`Order.Contracts.Fulfillment.StorefrontGuestActor`).
Zero foreign module `Application`/`Infrastructure`/`Domain`/`Endpoints` reference, zero foreign
DbContext, zero cross-module persistence reach-through, zero cross-module EF/SQL join, zero
`TypeForwardedTo`, zero namespace alias. `microserviceExtractable = true`.

## 7. Structure (defense in depth; authority stays W2)

`PROFESSIONAL_SHALLOW` capability-first folders; `Path-Namespace-State = EXACT` for every production
file; `Root-Allowlist-State = ENFORCED` (Contracts/Domain/Application empty; Endpoints
`UserPreferenceEndpointModule.cs`; Infrastructure `UserPreferenceModule.cs`); `Physical-Copy-State =
CLEAN`; `Solution-Explorer-State = CANONICAL` (`/Modules/UserPreference/` with exactly five
projects); `File-Cohesion-State = COHESIVE`; `God-File-State = NONE`.

## 8. Schema / migrations preserved

The `user_preference` schema, `user_preferences` (PK `OwnerUserId`) and `ui_preferences` (PK
`PreferenceId`, unique `(ActorUserId, Key)`) tables, and both migrations
`20260827215300_InitialUserPreference` and `20260828020000_AddUiPreferences` (+ designers + snapshot)
are byte-identical to the W0 baseline. The outbox `Translate`/`ResolveEventClrType` still return
`null` (no external event this version). No migration, designer, snapshot or DbContext file was
touched by any wave.

## 9. Host final closure preserved

Zero Host production file added, moved or widened by W3 (W1 touched no Host file; W2 touched none).
`HOST_TMAR_EVACUATION_FINAL_CLOSURE_CERTIFIED` / `HOST_ROOT_FINAL_CERTIFIED` are untouched;
`lastAcceptedTask` stays `TB-TMAR-HOST-ROOT-FINAL-CERT-001` and `currentHostCheckpoint` stays
`HOST_ROOT_FINAL_CERTIFIED`.

## 10. Manifest certification

The existing `UserPreference` `modules[]` entry was **refreshed in place** (not duplicated):

```text
structureCertified       : true                 (unchanged)
lockVersion              : ARCH-COMPLETE-002    (unchanged)
structureAuthorityTask   : TB-TMAR-USERPREFERENCE-AMSC-001-W2
structureAuthorityCommit : 2547037e79cb2154872c0bfef036ee812c4b037b
structureHandoffState    : READY_FOR_CERTIFY_CONSUMED_BY_W3
certificationState       : USERPREFERENCE_AMSC_001_CERTIFIED
currentCertificationTask : TB-TMAR-USERPREFERENCE-AMSC-001-W3
currentVerdict           : COMPLETE_REFERENCE_PATTERN
certificationNote        : AMSC-001 W0→W3 lineage + AMC-001 W4 historical note
```

`preCertModules` stays empty; UserPreference is correctly absent from
`uncertifiedHttpOwningModules`; `structureLock.certifiedModules` stays **31** with UserPreference
present exactly once.

## 11. Guards strengthened, never weakened

- New `UserPreferenceModuleAmsc001W3CertGuardTests` (**9 facts**) locks the manifest/SoT
  certification and wave lineage, the HTTP_OWNING six-route ownership, the CQRS 4/4 shape and the
  exhaustive validator matrix, the single stable-code owner with the declared-code guard, the
  bilingual locked resources and composed-catalog uniqueness, the canonical dual-mechanism typed-fault
  seam and the exactly-once composition-root registration, the Contracts-only microservice boundary,
  the unchanged schema/migration set, the structure invariants and the preserved Host closure +
  evidence tree.
- W1 guard (**8/8**) and W2 guard (**9/9**) re-run green — W1 semantics preserved, not weakened.
- No assertion was deleted, relaxed or repointed; no baseline widened.

## 12. Verification

```text
dotnet build Tooba.Host.Tests.csproj                 -> SUCCEEDED, 0 errors
dotnet test --filter UserPreferenceModuleAmsc001*
             | UserPreferenceModuleAmc* | HostPreferencesAmcGuardTests
             | ErrorCatalogUniqueCodeGuardTests       -> Failed: 0, Passed: 40, Skipped: 0

dotnet test --filter TmarCompleteReferenceStructureGateTests
             | TmarDurableGuardTests | ErrorCatalogUniqueCodeGuardTests
             | UserPreference* | HostPreferencesAmcGuardTests
             | TaxModuleAmsc001*                      -> 83 total, 76 passed, 4 skipped, 3 failed
```

**Baseline proof (`NEW = 0`).** The same filter was run in an isolated `git worktree` at the W2 head
`2547037e`: **66 total / 63 passed / 0 skipped / 3 failed** — the **identical** three failing test
ids:

```text
TmarCompleteReferenceStructureGateTests.Certified_modules_satisfy_root_allowlists_and_namespace_alignment
TmarDurableGuardTests.Recovery_current_state_is_fresh_and_machine_readable
TmarDurableGuardTests.Recovery_sot_sync_001_current_checkpoint_is_unique_and_stop_is_authoritative
```

All three are repository-global and pre-existing (the first is the documented out-of-scope
`Tooba.Catalog.Contracts/Cart` + `Tooba.Cart.Contracts/{Checkout,Presentation}` namespace deviation;
the other two are repository-global recovery pins unrelated to UserPreference). **Zero new
failures, zero regressions, zero UserPreference test failures.**

```text
Applicability               : HTTP_OWNING
Endpoint-Ownership-State    : MODULE_OWNED (6 routes / 3 groups, Host 0)
CQRS-State                  : COMPLIANT_4_OF_4
Validator-Matrix-State      : EXHAUSTIVE_4_OF_4
Stable-Error-Code-State     : SINGLE_CANONICAL_CONTRACTS_HOME_10_DECLARED_10_REGISTERED_10_LOCALIZED
Declared-Code-Guard-State   : PRESENT
Typed-Fault-Seam-State      : CANONICAL_DUAL_MECHANISM
Raw-Prose-Fault-State       : ZERO
Localization-State          : CANONICAL_10_EN_10_FA_EXPLICIT_LOCKED_LOGICAL_NAMES
Path-Namespace-State        : EXACT
Root-Allowlist-State        : ENFORCED
Alias-Workaround-State      : NONE
Folder-Granularity-State    : PROFESSIONAL_SHALLOW
Solution-Explorer-State     : CANONICAL
Physical-Copy-State         : CLEAN
Foreign-App-Infra-Domain    : ZERO
Cross-Module-Join-State     : ZERO
Schema-State                : UNCHANGED
Host-Final-Closure-State    : HOST_TMAR_EVACUATION_FINAL_CLOSURE_CERTIFIED_PRESERVED
Microservice-Extractable    : TRUE_CONTRACTS_ONLY_SELF_CONTAINED_ERROR_SURFACE_EXACT_PATH_NAMESPACE
Guards-Weakened             : NONE
Baselines-Widened           : NONE
Verdict                     : COMPLETE_REFERENCE_PATTERN
```

## 13. Stop gate

`USER_REVIEW_USERPREFERENCE_AMSC_001_W3`; `automaticNextImplementationTask = NONE`; own cert commit
SHA reported through the Result channel only.
