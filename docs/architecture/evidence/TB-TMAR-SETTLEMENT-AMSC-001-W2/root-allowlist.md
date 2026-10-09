# Root allowlists / forbidden root (skill §19)

`Root-Allowlist-State = ENFORCED`

Manifest source of truth: `docs/architecture/tmar-module-structure-manifests.json` → `modules[]` entry
`Settlement` (project entries for Application, Endpoints, Infrastructure — the three projects that have
a project root; Contracts, Domain and Tests have no project-root `.cs` and are therefore not listed,
matching the Returns convention).

## Manifest ↔ disk equality (verbatim, re-derived this wave)

### Tooba.Settlement.Application

```json
"rootAllowlist": [],
"forbiddenRootFiles": [
  "SettlementContracts.cs", "SettlementCommands.cs", "SettlementQueries.cs", "SettlementHandlers.cs",
  "SettlementErrorCodes.cs", "SettlementAdminModels.cs", "RequestSellerPayoutCommand.cs",
  "QueryAdminPayoutGridQuery.cs", "GlobalUsings.Domain.cs", "GlobalUsings.Layout.cs"
],
"forbiddenTopLevelFolders": ["Commands","Queries","Models","Ports","Validators","Errors","Handlers","Requests"]
```

Disk: **0** project-root `.cs`. None of the forbidden files exist. None of the forbidden folders exist
(the current top-level folders are `Composition`, `Payouts`, `Validation`).

### Tooba.Settlement.Endpoints

```json
"rootAllowlist": ["SettlementEndpointModule.cs"],
"forbiddenRootFiles": [
  "SettlementSellerEndpoints.cs", "SettlementAdminEndpoints.cs",
  "ISettlementSellerAuthorizer.cs", "ISettlementAdminAuthorizer.cs"
],
"forbiddenTopLevelFolders": []
```

Disk: exactly one project-root `.cs` — `SettlementEndpointModule.cs` (route mapping +
`AddSettlementEndpointPresentation` + the two authorizer registrations). Equality holds.

### Tooba.Settlement.Infrastructure

```json
"rootAllowlist": [],
"forbiddenRootFiles": [
  "SettlementModule.cs", "SettlementDbContext.cs", "SettlementDirectory.cs",
  "SettlementOutboxRegistration.cs", "SettlementEventHandlers.cs", "AdminPayoutGridQueryEngine.cs",
  "GlobalUsings.Domain.cs", "GlobalUsings.Layout.cs"
],
"forbiddenTopLevelFolders": ["Migrations","Repositories"]
```

Disk: **0** project-root `.cs`. Migrations live only under `Persistence/Migrations`; no `Repositories`
folder exists (persistence is directory/outbox based).

## What W2 changed in the manifest

1. **Normalized the indentation** of the three Settlement project entries — the W1 edit had left
   `projectName`/`rootAllowlist`/`forbiddenRootFiles` at the wrong indent level inside the object
   (valid JSON, but inconsistent with the other 28 certified entries).
2. **Added `certificationNote` + `evidence`** to the Settlement entry, matching every other certified
   entry; the W1 commit had removed the W0 note without adding a replacement, leaving Settlement as the
   only certified module with no note.
3. **Populated `forbiddenTopLevelFolders`** for Application (`Commands`, `Queries`, `Models`, `Ports`,
   `Validators`, `Errors`, `Handlers`, `Requests`) and Infrastructure (`Migrations`, `Repositories`) so
   the retired technical-axis-first roots are durably locked, matching the Returns/Story convention.

`structureCertified` and `lockVersion` were left `true` / `ARCH-COMPLETE-002` (the module is already
certified under ARCH-COMPLETE-002; the note now honestly records the AMSC-001 W3 re-certification).
No allowlist was widened to hide debt — `rootAllowlist` remains `[]` for Application and Infrastructure.

## Durable enforcement

`SettlementModuleAmsc001W2StructureGuardTests.Root_allowlists_match_disk_and_forbidden_entries_are_absent`
asserts exact set equality between each manifest `rootAllowlist` and the on-disk project-root `.cs`
files, and asserts every `forbiddenRootFiles` / `forbiddenTopLevelFolders` entry is absent.

## Host final closure (skill §22)

| Checkpoint | State |
|---|---|
| `HOST_TMAR_EVACUATION_FINAL_CLOSURE_CERTIFIED` | preserved |
| `HOST_ROOT_FINAL_CERTIFIED` | preserved |

- `src/backend/Host/Tooba.Host/Settlement/` does not exist.
- `src/backend/Host/Tooba.Host/Grid/` does not exist.
- Host contains **zero** `/settlements` route literals and zero Settlement DbContext reference.
- Host residue is exactly `ALLOWED_COMPOSITION_ROOT` (`Program.cs`: CQRS assembly registration +
  `AddSettlementEndpointPresentation()` + `MapSettlementEndpoints()` + the two authorizer
  registrations) and `ALLOWED_SECURITY_ADAPTER`
  (`Security/Seller/HostSettlementSellerAuthorizer.cs`,
  `Admin/Access/Authorizers/HostSettlementAdminAuthorizer.cs`).
- W2 created **no** new Host production file or folder.
