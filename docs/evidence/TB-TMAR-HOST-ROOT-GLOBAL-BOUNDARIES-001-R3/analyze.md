# TB-TMAR-HOST-ROOT-GLOBAL-BOUNDARIES-001-R3 — Analyze

Skills: `tooba-architecture-analyze` → `tooba-architecture-migrate` → `tooba-architecture-certify`.

## 1. Context

R2 accepted the Order dependency cleanup but **rejected R2 certification** because the newly created
`Tooba.AccessControl.Contracts` project has two structural blockers:

### BLOCKER 1 — path/namespace mismatch

- File: `src/backend/Modules/AccessControl/Tooba.AccessControl.Contracts/Access/AccessControlEffectiveAccessContracts.cs`
- Declared namespace: `Tooba.AccessControl.Contracts`
- Required (exact path-derived): `Tooba.AccessControl.Contracts.Access`

`ARCH-COMPLETE-002` rule `PATH_NAMESPACE_ALIGNMENT` requires namespace = project name + relative
folder path. The bare namespace was a violation.

### BLOCKER 2 — structure manifest

`Tooba.AccessControl.Contracts` was added to an already `ARCH-COMPLETE-002` certified module but
`docs/architecture/tmar-module-structure-manifests.json` had no entry for it. The gate
`TmarCompleteReferenceStructureGateTests` asserts that exact-real manifest entry count matches and that
root allowlists match disk.

## 2. Manifest conventions discovered

`Tooba.X.Contracts` projects are declared with `rootAllowlist: []` and a
`rootAllowlistJustification` (e.g. `Tooba.Fulfillment.Contracts`, `Tooba.AddressBook.Contracts`,
`Tooba.Content.Contracts`, `Tooba.ProductWorkspace.Contracts`). The `AccessControl` module must remain
a **single** module entry; the Container project is one more `projects[]` element inside it.

The AccessControl module's existing project entries:

```text
Tooba.AccessControl.Application
Tooba.AccessControl.Endpoints
Tooba.AccessControl.Infrastructure
```

## 3. Scope decision

Structural closure only:

- repoint the namespace and every consumer (no alias, no shim, no duplicate contract file);
- add exactly one `Tooba.AccessControl.Contracts` project entry to the existing single AccessControl
  module manifest entry;
- update SoT + evidence;
- re-run focused validation.

Not in scope: AccessControl redesign, authorization/SpiceDB behavior, routes, schema, frontend,
any other Host folder.

## 4. Consumers to repoint

| Consumer | File |
| -------- | ---- |
| Order.Infrastructure fulfillment permission gate | `Admin/Fulfillment/AdminOrderFulfillmentPermissionGate.cs` |
| AccessControl adapter | `Adapters/AccessControlEffectiveAccessReader.cs` |
| AccessControl DI registration | `AccessControlModule.cs` |

`Tooba.Order.Infrastructure.csproj` and `Tooba.AccessControl.Infrastructure.csproj` project
*references* keep the assembly/project name (`Tooba.AccessControl.Contracts`) — unchanged and correct.
