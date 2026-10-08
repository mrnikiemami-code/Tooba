# Root-Allowlist-State

**Verdict: `ENFORCED`** · Host final closure: **`PRESERVED`**

## 1. Disk truth (production `.cs` at project root, non-recursive)

```text
Tooba.Returns.Application       => (none)
Tooba.Returns.Contracts         => (none)
Tooba.Returns.Domain            => (none)
Tooba.Returns.Endpoints         => ReturnEndpointModule.cs
Tooba.Returns.Infrastructure    => (none)
Tooba.Returns.Tests             => (none)
```

## 2. Manifest agreement

`docs/architecture/tmar-module-structure-manifests.json` → `preCertModules[module=Returns]`:

| Project | `rootAllowlist` | Disk root `.cs` | Match |
|---|---|---|---|
| `Tooba.Returns.Contracts` | `[]` | none | YES |
| `Tooba.Returns.Domain` | `[]` | none | YES |
| `Tooba.Returns.Application` | `[]` | none | YES |
| `Tooba.Returns.Infrastructure` | `[]` | none | YES |
| `Tooba.Returns.Endpoints` | `["ReturnEndpointModule.cs"]` | `ReturnEndpointModule.cs` | YES |
| `Tooba.Returns.Tests` | `[]` | none | YES |

## 3. Forbidden root files (must never appear)

`Tooba.Returns.Contracts`: `ReturnsContracts.cs`, `ReturnsErrorCodes.cs`,
`ReturnsErrorResourceSet.cs`, `ReturnAdminOperationsContracts.cs`
→ all absent; the canonical homes are `Errors/`, `Operations/`.

`Tooba.Returns.Domain`: `ReturnRequest.cs`, `ReturnDomainEvents.cs`, `ReturnRequestStatus.cs`
→ all absent; the canonical homes are `Aggregates/`, `Events/`, `ValueObjects/`.

`Tooba.Returns.Application`: `ReturnsOperation.cs`, `ReturnRefundDestinationParser.cs`,
`ReturnsRequestValidators.cs`, `ReturnsValidationCodes.cs`, `CreateReturnCommand.cs`
→ all absent; the canonical homes are `Composition/`, `Validation/`, `ReturnRequests/Commands/`.

`Tooba.Returns.Infrastructure`: `ReturnsModule.cs`, `ReturnsDbContext.cs`, `ReturnDirectory.cs`,
`ReturnsOutboxRegistration.cs`, `AdminReturnGridQueryEngine.cs`
→ all absent; the canonical homes are `DependencyInjection/`, `Persistence/`, `Directories/`,
`Messaging/`, `Queries/`.

`Tooba.Returns.Endpoints`: `ReturnAdminEndpoints.cs`, `ReturnSellerEndpoints.cs`,
`ReturnCustomerEndpoints.cs`, `ReturnsErrorCatalogContributor.cs`
→ all absent; the canonical homes are `Admin/`, `Seller/`, `Customer/` and `Infrastructure/Errors/`.

## 4. Forbidden top-level folders (must never appear)

| Project | Forbidden | Present |
|---|---|---|
| `Tooba.Returns.Application` | `Commands`, `Queries`, `Models`, `Ports`, `Validators`, `Errors`, `Handlers`, `Requests` | NO |
| `Tooba.Returns.Infrastructure` | `Migrations` (must live under `Persistence/Migrations/`), `Repositories` | NO |
| others | `[]` | n/a |

## 5. Root-Dump check (section 19)

| Check | Result |
|---|---|
| Capability/implementation `.cs` at any project root | 0 |
| Capability `*Endpoints.cs` at Endpoints root | 0 |
| DbContext at Infrastructure root | 0 |
| Migrations at Infrastructure root | 0 |
| Allowlist widened to hide debt | NO (all `rootAllowlist` are `[]` except the justified composition entry) |

## 6. Host final closure (section 22)

| Checkpoint | State |
|---|---|
| `HOST_TMAR_EVACUATION_FINAL_CLOSURE_CERTIFIED` | PRESERVED |
| `HOST_ROOT_FINAL_CERTIFIED` | PRESERVED |
| New Host production folder/file added by W2 | 0 |
| Module business/persistence moved into Host by W2 | 0 |
| Host-owned Returns residue | 0 (only `MapReturnEndpoints()` invocation + 2 module-owned authorizer seam implementations, `ALLOWED_SECURITY_ADAPTER`) |
| `HOST_FINAL_CLOSURE_REGRESSION` | NOT TRIGGERED |

## 7. Enforcement

`ReturnsModuleAmsc001W2StructureGuardTests.Root_allowlists_match_disk_and_forbidden_entries_are_absent`
compares the manifest allowlist to the actual root file list and asserts every forbidden root file and
forbidden top-level folder is absent. The manifest and disk can no longer silently drift apart.
