# Manifest state + SoT — AccessControl (W3)

## Manifest

`docs/architecture/tmar-module-structure-manifests.json`

| Check | Result |
| --- | --- |
| Entries for `AccessControl` | exactly 1 |
| `structureCertified` | `true` |
| `lockVersion` | `ARCH-COMPLETE-002` |
| Temporary pre-cert duplicate | none |
| Project set vs disk | `EXACT` (5 projects) |
| Per-project `rootAllowlist` vs disk root `.cs` | `EXACT` |
| `forbiddenRootFiles` present on disk | `ZERO` |
| `forbiddenTopLevelFolders` present on disk | `ZERO` |

### Entry summary

| Project | `rootAllowlist` | `forbiddenRootFiles` | `forbiddenTopLevelFolders` |
| --- | --- | --- | --- |
| `Tooba.AccessControl.Application` | `[]` | 17 declared | `["Exceptions","Validators"]` |
| `Tooba.AccessControl.Contracts` | `[]` | 5 declared | `[]` |
| `Tooba.AccessControl.Domain` | `[]` | 1 declared | `[]` |
| `Tooba.AccessControl.Endpoints` | `["AccessControlEndpointModule.cs"]` | 3 declared | `[]` |
| `Tooba.AccessControl.Infrastructure` | `["AccessControlModule.cs"]` | 3 declared | `[]` |

No manifest value was changed in W3 — W1/W2 had already made the entry honest, and W2 added durable
enforcement rather than a manifest edit. Verified honest, not merely present.

## SoT

`docs/architecture/tmar-current-state.json`

| Record | State |
| --- | --- |
| `accessControlStructureRecert001` | `ACCESSCONTROL_STRUCTURE_RECERTIFIED` (preserved) |
| `accessControlModuleAmsc001W0` | `READY_TO_MIGRATE` (preserved) |
| `accessControlModuleAmsc001W1` | `ACCESSCONTROL_COHESION_MIGRATED` (preserved) |
| `accessControlModuleAmsc001W2` | `ACCESSCONTROL_STRUCTURE_NORMALIZED` (preserved) |
| `accessControlModuleAmsc001W3` | `COMPLETE_REFERENCE_PATTERN` (added by this task) |

### `structureLock`

| Field | Value |
| --- | --- |
| `version` | `ARCH-COMPLETE-002` |
| `certifiedModules` contains `AccessControl` | ✅ |
| `manifest` | `docs/architecture/tmar-module-structure-manifests.json` |
| `standard` | `docs/architecture/TMAR-COMPLETE-REFERENCE-STRUCTURE-STANDARD.md` |

### Host checkpoints

| Field | Value |
| --- | --- |
| `currentHostCheckpoint` | `HOST_ROOT_FINAL_CERTIFIED` |
| `hostRootFinalCheckpoint` (AccessControl records) | `PRESERVED` |

## Honesty statement

- No unrelated history was rewritten.
- No prior AccessControl record was deleted or repurposed.
- The three pre-existing repo-wide guard drifts (`Catalog` module expectation, `Catalog` Cart-contract
  namespace, stale `structureLock.certifiedModules` expectation) are recorded honestly as
  **out of scope**, not hidden or suppressed. See `residual-debt.md`.
- No baseline, allowlist or guard was widened to make certification pass.
