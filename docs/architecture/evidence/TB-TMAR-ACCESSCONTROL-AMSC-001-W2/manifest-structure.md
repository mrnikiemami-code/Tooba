# Manifest structure — AccessControl (W2)

## Classification

| Axis | State |
| --- | --- |
| Manifest entry count for `AccessControl` | exactly `1` |
| `structureCertified` | `true` |
| `lockVersion` | `ARCH-COMPLETE-002` |
| Manifest ↔ disk project set | `EXACT` |
| Manifest `rootAllowlist` ↔ disk root `.cs` | `EXACT` (all 5 projects) |
| Manifest `forbiddenRootFiles` present on disk | `ZERO` |
| Manifest `forbiddenTopLevelFolders` present on disk | `ZERO` |
| Allowlist widening to hide debt | `NONE` |

## Entry ↔ disk reconciliation

| Project | Manifest `rootAllowlist` | Disk root `.cs` | Match |
| --- | --- | --- | --- |
| `Tooba.AccessControl.Application` | `[]` | — | ✅ |
| `Tooba.AccessControl.Contracts` | `[]` | — | ✅ |
| `Tooba.AccessControl.Domain` | `[]` | — | ✅ |
| `Tooba.AccessControl.Endpoints` | `["AccessControlEndpointModule.cs"]` | `AccessControlEndpointModule.cs` | ✅ |
| `Tooba.AccessControl.Infrastructure` | `["AccessControlModule.cs"]` | `AccessControlModule.cs` | ✅ |

Project set declared in the manifest:

```text
Tooba.AccessControl.Application
Tooba.AccessControl.Contracts
Tooba.AccessControl.Domain
Tooba.AccessControl.Endpoints
Tooba.AccessControl.Infrastructure
```

Project directories on disk: exactly the same five. `Tooba.AccessControl.Tests` does **not**
exist, and the manifest correctly does not declare it (W0 finding `F5`, out of scope).

## W1 manifest honesty re-verified

W1 widened `Tooba.AccessControl.Application.forbiddenRootFiles` and added
`forbiddenTopLevelFolders: ["Exceptions", "Validators"]`. W2 confirms this is **honest** and not
allowlist widening:

- `Exceptions/` and `Validators/` are genuinely absent from disk.
- Every one of the 17 declared `forbiddenRootFiles` is genuinely absent from disk.
- `rootAllowlist` was **not** widened — it stayed `[]` for Application.

No manifest value was changed in W2. The `AccessControl` entry already described the real
certified surface, so W2 added **durable enforcement** instead of a manifest edit.

## New W2 durable guard

`src/backend/Host/Tooba.Host.Tests/Architecture/AccessControlManifestDiskReconciliationGuardTests.cs`
reads the real manifest, the real project directories and the real `Tooba.slnx`, and fails if:

1. the `AccessControl` entry is missing, duplicated, uncertified or on another lock version;
2. the declared project set differs from the project directories on disk (missing **or** extra);
3. any declared `rootAllowlist` differs from the real top-level production `.cs` files;
4. the `/Modules/AccessControl/` solution folder does not reference exactly those five projects,
   or any entry points at a non-existent `.csproj`;
5. `Application/Commands` or `Application/Queries` reappears as a technical-axis-first root;
6. `Exceptions/` or `Validators/` is resurrected, or `Validation/` disappears;
7. a single-file request/use-case leaf folder is introduced under any `Commands/` or `Queries/`;
8. any production path no longer matches its declared namespace exactly.

All eight facts pass.

## Pre-existing, out-of-scope repo-wide drift (reported, not repaired)

Two **shared, repo-wide** guards fail on `main` **independently of AccessControl** and
independently of W0/W1/W2. This was proven, not assumed:

| Probe | Revision | `TmarCompleteReferenceStructureGateTests` |
| --- | --- | --- |
| clean `HEAD` (`c9e009f8`, W1) | W1 tip | `Failed: 3 / 3` |
| W0 baseline (`a3ba1a4f`) | before any AccessControl AMSC work | `Failed: 3 / 3` |

Root causes (both outside the AccessControl surface):

1. `TmarCompleteReferenceStructureGateTests` expects a 21-module manifest set that omits
   `Catalog`, while `tmar-module-structure-manifests.json` now declares 22 modules
   (`Catalog` added by `412ec244 docs(tmar): Catalog AMC-001 W4 certify …`; `ProductWorkspace`
   and `Story` also present).
2. The same gate asserts namespace alignment for `Tooba.Catalog.Contracts`, which contains
   `Cart/CatalogCartQuantityContracts.cs` and `Cart/ICatalogCartPresentationLookup.cs` declaring
   `namespace Tooba.Catalog.Contracts` instead of `Tooba.Catalog.Contracts.Cart`
   (introduced by `56692b7c`, Cart golden closure).
3. `TmarDurableGuardTests.Recovery_current_state_is_fresh_and_machine_readable` still asserts a
   stale 16-module `structureLock.certifiedModules` list, while SoT now holds 21 entries.

None of these three defects is caused by, or repairable from, the AccessControl module. Repairing
them would mean editing the shared structure gate, the `Catalog` module and the repo-wide SoT
outside this task's authorized scope. They are therefore recorded here as **out-of-scope
pre-existing drift** and left untouched.

AccessControl-scoped structural enforcement (all eight W2 guard facts plus the W1 cohesion guard
plus the pre-existing AccessControl AMC/RECERT/Repair guards) is **fully green**.
