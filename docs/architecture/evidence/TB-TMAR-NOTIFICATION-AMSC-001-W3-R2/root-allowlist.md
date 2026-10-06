# TB-TMAR-NOTIFICATION-AMSC-001-W3-R2 — root-allowlist

Manifest physical truth (`docs/architecture/tmar-module-structure-manifests.json`,
`Tooba.Notification.Application` entry): `rootAllowlist = []`, `forbiddenRootFiles = []`,
`forbiddenTopLevelFolders = ["Commands", "Queries", "Errors", "Services"]`.

## Verification

- Application project root has zero `.cs` files (root allowlist `[]` enforced).
- No `Application/Commands`, `Application/Queries`, `Application/Errors` or `Application/Services`
  directory exists at the project root (`NotificationModuleAmsc001W3CertGuardTests.Application_is_capability_first_with_no_technical_axis_roots`).
- Root-Allowlist-State = `ENFORCED` before and after the repair — the repair moved files deeper into
  existing `Customer/Seller` capability branches and did not touch any project root.
- All other projects (Contracts/Domain/Infrastructure/Endpoints root allowlists incl. the
  `Endpoints` root `NotificationEndpointModule.cs` entry) unchanged — verified by the unchanged
  `NotificationArchitectureGuardTests` root-dump assertions.
- `Tooba.Notification.Endpoints` root files remain exactly `NotificationEndpointModule.cs`.

## Manifest change decision

The manifest's Application entry forbids the root-level technical axes and carries no per-leaf path
declarations, so the flattened tree does not contradict any manifest record: the physical allowlist
truth did not require a manifest edit, certification membership was not altered, and
`structureCertified` stays untouched (this is a Structure repair, not a Certify wave). The manifest
was therefore NOT modified in W3-R2, per the task's manifest rule.
