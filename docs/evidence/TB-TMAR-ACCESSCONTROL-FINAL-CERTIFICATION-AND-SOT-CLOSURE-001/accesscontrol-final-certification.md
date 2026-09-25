# AccessControl — Final ARCH-COMPLETE-002 Certification + Recovery SoT Closure

Task: `TB-TMAR-ACCESSCONTROL-FINAL-CERTIFICATION-AND-SOT-CLOSURE-001`
Parent: `TB-TMAR-ACCESSCONTROL-PRECERT-VALIDATORS-001` (Architect-ACCEPTED at `34476274bcb116e28de7ae96977e819d4f6d09eb`)
Recovery SoT pre-check commit: `ad63ed23f27cd342d00e1c058458aadc2d08bfe6`
Scope: docs + guards only. No production code change. No schema/migration change. No route change. No new validators. No Host AccessControl recreation. No AddressBook work.

## 1. Verdict

AccessControl is **COMPLETE_REFERENCE_PATTERN** and **ARCH-COMPLETE-002 STRUCTURE_CERTIFIED**.

All AccessControl-specific certification gates pass. No AccessControl-specific blocker remains.

## 2. Host ZERO proof

- `src/backend/Host/Tooba.Host/AccessControl` directory: **ABSENT** (`Test-Path` = `False`).
- `namespace Tooba.Host.AccessControl`: **ZERO** occurrences anywhere under `src/backend/Host/Tooba.Host`.
- `MapAccessControlEndpoints` (legacy Host map): **ZERO** in `Program.cs`; the only map call is `app.MapAccessControlModuleEndpoints()` at `Program.cs:513`.
- The single composition seam is `builder.Services.AddScoped<Tooba.BuildingBlocks.Security.IPlatformEffectiveAccessReader, Tooba.AccessControl.Infrastructure.Adapters.Security.PlatformEffectiveAccessReader>()` (`Program.cs:203`), plus the CQRS foundation assembly registration `typeof(EnsureAccessControlBootstrapCommand).Assembly` (`Program.cs:156`).
- Guard: `AccessControlFoundationTests.AccessControl_module_boundary_static_checks` asserts the Host folder does not exist, the legacy map call is absent, and no Host file carries `namespace Tooba.Host.AccessControl`, `AccessControlDevelopmentSeed`, or `AccessControlDemoSnapshot`.

## 3. Endpoint ownership proof

- HTTP ownership is module-owned: `Tooba.AccessControl.Endpoints` (`Admin/AccessControlAdminEndpoints.cs`, `Admin/AccessControlAdminSellerEndpoints.cs`, `Seller/AccessControlSellerEndpoints.cs`).
- All three endpoint files resolve and dispatch through a real MediatR 12.5 `ISender`.
- Endpoints project references only `Tooba.AccessControl.Application` + `Tooba.BuildingBlocks` — no `Tooba.Host`, no `.Infrastructure`.
- Host duplicate route ownership = **ZERO**.
- Endpoints → direct `IAccessControlDirectory` = **ZERO**.

## 4. Structure / root allowlists

| Project | Actual root `.cs` | rootAllowlist |
| --- | --- | --- |
| `Tooba.AccessControl.Application` | *(none)* | `[]` |
| `Tooba.AccessControl.Endpoints` | `AccessControlEndpointModule.cs` | `["AccessControlEndpointModule.cs"]` |
| `Tooba.AccessControl.Infrastructure` | `AccessControlModule.cs` | `["AccessControlModule.cs"]` |

`Tooba.AccessControl.Domain` root is `AccessControlDomain.cs` (not part of the task-mandated allowlist set; not certified as a flattened root).

Forbidden flattened root files encoded in the manifest and asserted absent by the structure gate:

- Application: `AccessControlContracts.cs`, `PermissionCatalog.cs`
- Endpoints: `AccessControlAdminEndpoints.cs`, `AccessControlAdminSellerEndpoints.cs`, `AccessControlSellerEndpoints.cs`
- Infrastructure: `AccessControlDirectory.cs`, `AccessControlInstrumentation.cs`, `AccessControlOutboxRegistration.cs`

Capability folders present: Application `Authorization/Commands/Models/Permissions/Queries/Validators`; Endpoints `Admin/Seller`; Infrastructure `Adapters/Directories/Messaging/Observability/Persistence`.

## 5. Path ↔ namespace proof

Exact path-derived namespace equality verified for every AccessControl production `.cs` file (41 files scanned, all `OK`):

- `Authorization\AccessControlCapabilityGate.cs` → `Tooba.AccessControl.Application.Authorization`
- `Commands\<Capability>\*Command.cs` → `Tooba.AccessControl.Application.Commands.<Capability>`
- `Models\AccessControlContracts.cs` → `Tooba.AccessControl.Application.Models`
- `Permissions\PermissionCatalog.cs` → `Tooba.AccessControl.Application.Permissions`
- `Queries\<Capability>\*Query.cs` → `Tooba.AccessControl.Application.Queries.<Capability>`
- `Validators\*` → `Tooba.AccessControl.Application.Validators[.<Folder>]`
- `Admin\*` → `Tooba.AccessControl.Endpoints.Admin`, `Seller\*` → `Tooba.AccessControl.Endpoints.Seller`
- `Adapters\Security\*`, `Directories\*`, `Messaging\*`, `Observability\*`, `Persistence\*` → matching `Tooba.AccessControl.Infrastructure.*`

Path↔namespace = **EXACT**. No namespace-alias workaround. No `TypeForwardedTo`.

Note: `Persistence/Migrations/*` project-local namespaces (`Tooba.AccessControl.Infrastructure.Persistence.Migrations`) are the legitimate EF migrations folder namespace and therefore intentionally not path-derived; they are outside the production `*.cs` certification target, same as the accepted Order/Cart/Offer/Payment/Settlement/Fulfillment precedent.

## 6. Exact 19 request inventory

Endpoint-reachable MediatR requests = **19**.

| Classification | Count | Requests |
| --- | --- | --- |
| VALIDATOR_REQUIRED | 6 | `CreateRoleCommand`, `UpdateRoleCommand`, `CloneRoleCommand`, `AssignRoleCommand`, `SetRolePermissionsCommand`, `SetSellerCeilingCommand` |
| NO_VALIDATOR_REQUIRED | 13 | remaining endpoint-reachable queries/commands with trusted/authorization-derived or no-input envelopes |

- `validatorRequiredCount` = 6
- `validatorsPresentCount` = 6
- `validatorsMissingCount` = 0
- `noValidatorRequiredCount` = 13
- validator gap = **ZERO**

## 7. Validator coverage + discovery

Validators present (all under `Tooba.AccessControl.Application/Validators/`):

- `Validators/Role/CreateRoleCommandValidator.cs` — Name non-blank ≤ 128, Code shape 2..64 `[a-zA-Z0-9_-]`, Description optional ≤ 512
- `Validators/Role/UpdateRoleCommandValidator.cs` — Name non-blank ≤ 128, Description optional ≤ 512
- `Validators/Role/CloneRoleCommandValidator.cs` — Name non-blank ≤ 128, Code shape, Description optional ≤ 512
- `Validators/Assignment/AssignRoleCommandValidator.cs` — UserId non-empty, RoleId non-empty
- `Validators/Permissions/SetRolePermissionsCommandValidator.cs` — Grants non-null, RoleId non-empty, each grant PermissionId non-blank ≤ 128
- `Validators/Ceiling/SetSellerCeilingCommandValidator.cs` — Entries non-null, SellerPartyId non-empty, each entry PermissionId non-blank ≤ 128

Reusable fragments: `Validators/AccessControlFluentRules.cs`; stable machine-readable codes: `Validators/AccessControlValidationCodes.cs`.

Discovery: automatic via the existing `AddToobaCqrsFoundation(...)` → `AddValidatorsFromAssembly(assembly)` for the already-registered `EnsureAccessControlBootstrapCommand` assembly. No custom pipeline, no manual invocation anywhere.

Scope discipline: transport/input shape only. Role existence/mutability, code conflict, ceiling, delegation, catalog membership, category existence, escalation, assignment uniqueness and ownership/state remain owned by `AccessControlDirectory` / `AccessControlException`. The focused guard asserts validators never reference `IAccessControlDirectory`, `Escalat*`, or `AccessControlException`.

## 8. Boundary audit

- `AccessControl.Application` → `Host`: **ZERO**
- `AccessControl.Application` → foreign Application/Domain: **ZERO**
- Allowed Contracts only: `Catalog.Contracts`, `Identity.Contracts`, `OperatorProfile.Contracts`
- `AccessControl.Infrastructure` → `Catalog.Application`/`Catalog.Domain`: **ZERO**; → `Catalog.Contracts`: **ALLOWED**
- `AccessControl.Endpoints` → `Host`: **ZERO**; → direct `IAccessControlDirectory`: **ZERO**
- `AccessControl.Infrastructure` → `Tooba.Host`: **ZERO**

## 9. Manifest entry proof

`docs/architecture/tmar-module-structure-manifests.json` now contains an `AccessControl` module entry:

```json
{
  "module": "AccessControl",
  "structureCertified": true,
  "lockVersion": "ARCH-COMPLETE-002"
}
```

with the three project root-allowlist/forbidden-root-file sets from §4. `uncertifiedHttpOwningModules` remains `["Returns","Notification","Support","Wallet","Promotion"]` — AccessControl is **not** listed. No unrelated forbidden entry was invented. Existing historical certified modules were not removed or reordered.

## 10. certifiedModules proof

`docs/architecture/tmar-current-state.json` → `structureLock.certifiedModules`:

```text
Order, Cart, StoreContext, Offer, Payment, Settlement, Fulfillment, AccessControl
```

Durable coherence guard `TmarDurableGuardTests.Recovery_current_state_is_fresh_and_machine_readable` asserts every `certifiedModules` entry has a matching manifest entry with `structureCertified = true` and is absent from `uncertifiedHttpOwningModules`.

## 11. COMPLETE_REFERENCE_PATTERN SoT proof

`tmar-current-state.json` → `accessControlArchComplete002Structure`:

- `state` = `COMPLETE_REFERENCE_PATTERN`
- `httpApplicability` = `HTTP_OWNING`
- `endpointOwnership` = `MODULE_ENDPOINTS`
- `cqrs` = `MEDIATR_12_5`
- `structureCertifiedUnderArchComplete002` = `true`
- `validatorCoverage` = `COMPLETE_6_OF_6_REQUIRED_PRESENT_13_NO_VALIDATOR_REQUIRED`
- `endpointReachableRequests` = `19`
- `hostAccessControlResidue` = `ZERO`
- `pathNamespace` = `EXACT`
- `rootAllowlist` = `ENFORCED`
- `contractsBoundary` = `CLEAN_CONTRACTS_ONLY`
- `moduleMap` = `MapAccessControlModuleEndpoints`

## 12. Next Host folder / task

`currentHostEvacuation` closed AccessControl and advanced the Host walk:

- `accessControlClosure` = `COMPLETE`
- `hostEvacuationState` = `ACCESS_CONTROL_COMPLETE_ADDRESSBOOK_PENDING`
- `activeModule` = `AddressBook`
- `nextHostFolderAfterAccessControl` = `AddressBook`
- `nextTask` / `currentTask` = `TB-TMAR-HOST-ADDRESSBOOK-INVENTORY-001`
- `nextTaskGate` = `HOST_FIRST_FOLDER_BY_FOLDER_AFTER_ACCESSCONTROL_FINAL_CERTIFICATION`

AddressBook Host folder confirmed present with exactly `AddressBookEndpoints.cs` and `AddressBookDevelopmentSeed.cs`; `app.MapAddressBookEndpoints()` is still in `Program.cs`. No AddressBook work was started.

## 13. Known repository-wide baseline debt (classified, not repaired)

- `src/backend/Host/Tooba.Host.Tests/Baselines/tmar-source-size-baseline.json` — stale
- `src/backend/Host/Tooba.Host.Tests/Baselines/tmar-infra-to-foreign-application.json` — stale
- `source-size-inventory.json` — stale

These are repository-wide and **not** AccessControl-specific certification blockers while the dedicated AccessControl structure/boundary/validator guards pass. Not modified in this task.

## 14. Focused guards run

- `AccessControlValidatorTests`
- `AccessControlFoundationTests.AccessControl_module_boundary_static_checks`
- existing exact AccessControl manifest/SoT guards: `TmarCompleteReferenceStructureGateTests`, `TmarDurableGuardTests`

No solution build. No broad integration or architecture suite. Docs-only changes required no build.

## 15. Protected state

- Frontend = FROZEN
- Checkout = `PAUSED_AT_SAFE_W5_CHECKPOINT`
- No production code change, no schema/migration change, no route change, no new validator, no Host AccessControl recreation, no AddressBook implementation work.

## 16. Guard alignment (exact deltas)

Only the durable SoT/manifest gate expectations were advanced; no new broad guard suite was created.

- `Architecture/TmarCompleteReferenceStructureGateTests.cs`
  - manifest module set now includes `AccessControl`
  - `uncertifiedHttpOwningModules` exclusion list now includes `AccessControl`
  - `Uncertified_modules_are_explicitly_not_claimed` asserts AccessControl absent from uncertified and present in `structureLock.certifiedModules`
- `TmarDurableGuardTests.cs`
  - `nextTask` = `TB-TMAR-HOST-ADDRESSBOOK-INVENTORY-001`
  - `nextTaskGate` = `HOST_FIRST_FOLDER_BY_FOLDER_AFTER_ACCESSCONTROL_FINAL_CERTIFICATION`
  - `structureLock.certifiedModules` now `AccessControl, Cart, Fulfillment, Offer, Order, Payment, Settlement, StoreContext`
  - new `accessControlArchComplete002Structure` assertions (state, HTTP applicability, endpoint ownership, CQRS, certification flag, 19/6/6/0/13 validator counts, path namespace, root allowlist, alias workaround, Host residue ZERO, module map, contracts boundary, manifestCertified, certifiedModules CSV, checkout/frontend, workflow, nextHostFolder)
  - new `currentHostEvacuation` closure assertions
  - repaired the lagging `lastAcceptedTask`/`lastAcceptedCommit` expectation (was asserting the superseded Fulfillment evacuation stamp `552c928c…` against an SoT that already advanced to `TB-TMAR-ACCESSCONTROL-PRECERT-VALIDATORS-001` @ `34476274…`) — this was the verified pre-existing guard/SoT drift that made `Recovery_current_state_is_fresh_and_machine_readable` red for *every* task after `TB-TMAR-ACCESSCONTROL-PRECERT-STRUCTURE-REPAIR-001`; it is not an AccessControl structural defect
  - `AccessControl` was intentionally **not** added to `completeReferenceModules` in this task, because the durable guard pins that array to exactly 12 entries and to the canonical 11-module HTTP manifest ordering; AccessControl certification is fully expressed in `accessControlArchComplete002Structure` + `structureLock.certifiedModules` + manifest, which is the closure the task specifies

Focused result: `Failed: 0, Passed: 20, Skipped: 1` (the single skip is the Docker/Testcontainers-gated `Ceiling_escalation_and_category_scope_policy`).

Focused build: `Tooba.Host.Tests` builds with **0 errors**.

## 17. Repository-wide baseline debt (unchanged, not repaired)

`tmar-source-size-baseline.json`, `tmar-infra-to-foreign-application.json` and `source-size-inventory.json` remain stale repository-wide baselines. They are explicitly out of scope for this AccessControl certification and were not modified.

