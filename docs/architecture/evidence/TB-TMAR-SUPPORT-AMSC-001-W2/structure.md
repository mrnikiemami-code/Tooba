# TB-TMAR-SUPPORT-AMSC-001 — Wave 2 (Structure)

- **Skill:** `tooba-architecture-structure` (V2)
- **Mode:** `ARCHITECT_DIRECT_AMSC`
- **Target:** `src/backend/Modules/Support/Tooba.Support.*`
- **Starting HEAD:** `5e8c86ef` (W1 migrate commit, `HEAD == origin/main`)
- **Branch:** `main`
- **Wave lineage:** W0 `567ac400` → W1 `5e8c86ef` → W2 this commit
- **Business behavior changed in this wave:** NONE. No route, verb, status code, response body, DTO
  shape, business rule, state transition, ordering, idempotency, transaction, schema, migration id,
  outbox event name, telemetry name or localization key changed. This is a physical/organization wave.

Final objective: Support must be extractable as an independent microservice. Structure is the
physical half of that; ownership/coupling was already settled by W1.

---

## Classification states

| Gate | State |
| --- | --- |
| Module Applicability Gate | `HTTP_OWNING` — 17 real module routes over `/v1/customer/support`, `/v1/seller/support`, `/v1/admin/support`; Host-owned Support route count **0** |
| Folder-Granularity-State | `PROFESSIONAL_SHALLOW` (was `TECHNICAL_AXIS_FIRST` + `OVER_FOLDERED`) |
| Solution-Explorer-State | `CANONICAL` |
| Path-Namespace-State | `EXACT` (0 mismatches over 47 production `.cs`) |
| Physical-Copy-State | `CLEAN` |
| Root-Allowlist-State | `ENFORCED` |
| File-Cohesion-State | `COHESIVE` |
| Structure-State | **`READY_FOR_CERTIFY`** |
| Host final closure | `PRESERVED` (no new Host production folder/file) |

---

## 1. Moves performed

| From | To |
| --- | --- |
| `Application/Commands/<UseCase>/<UseCase>Command.cs` (10 single-file leaves) | `Application/Tickets/Commands/<UseCase>Command.cs` |
| `Application/Queries/<UseCase>/<UseCase>Query.cs` (7 single-file leaves) | `Application/Tickets/Queries/<UseCase>Query.cs` |
| `Application/Models/*.cs` (3) | `Application/Tickets/Models/` |
| `Application/Ports/*.cs` (2) | `Application/Tickets/Ports/` |
| `Domain/ValueObjects/SupportEnums.cs` | `Domain/Enums/SupportEnums.cs` |
| `Infrastructure/Migrations/*.cs` (3) | `Infrastructure/Persistence/Migrations/` |
| `Infrastructure/Seeds/SupportDevelopmentSeed.cs` | `Infrastructure/Development/SupportDevelopmentSeed.cs` |

Retired folders (removed, and now forbidden in the manifest): `Application/Commands`,
`Application/Queries`, `Application/Models`, `Application/Ports`, `Domain/ValueObjects`,
`Infrastructure/Migrations`, `Infrastructure/Seeds`. `Application/Errors` was already retired in W1.

All 27 moved files are staged as git renames (R073–R099), so history follows the move and no
stale physical copy remains.

---

## 2. Namespace consequences

- `Tooba.Support.Application.Commands.<UseCase>` → **`Tooba.Support.Application.Tickets.Commands`**
- `Tooba.Support.Application.Queries.<UseCase>` → **`Tooba.Support.Application.Tickets.Queries`**
- `Tooba.Support.Application.Models` → **`Tooba.Support.Application.Tickets.Models`**
- `Tooba.Support.Application.Ports` → **`Tooba.Support.Application.Tickets.Ports`**
- `Tooba.Support.Domain.ValueObjects` → **`Tooba.Support.Domain.Enums`**
- `Tooba.Support.Infrastructure.Migrations` → **`Tooba.Support.Infrastructure.Persistence.Migrations`**
- `Tooba.Support.Infrastructure.Seeds` → **`Tooba.Support.Infrastructure.Development`**

`Commands`/`Queries` are flat shallow axes (one namespace per axis, no per-use-case namespace) —
exactly the certified `Returns`/`Story`/`Promotion` precedent, where `ReturnRequests/Commands`
holds 4 flat command files under one namespace.

The only **non-Support** production file touched is one line of Host composition:

```text
src/backend/Host/Tooba.Host/Program.cs:178
-  typeof(Tooba.Support.Application.Commands.CreateCustomerTicket.CreateCustomerTicketCommand).Assembly,
+  typeof(Tooba.Support.Application.Tickets.Commands.CreateCustomerTicketCommand).Assembly,
```

That is the CQRS assembly registration anchor and must follow the type it names. No Host file or
folder was added; no Support responsibility moved into Host.

---

## 3. Consumed downstream surface verified

- `Host/Tooba.Host/Program.cs` — the only Host consumer of a Support Application type (above).
- Host authorizers (`HostSupportSellerAuthorizer`, `HostSupportAdminAuthorizer`) and the
  composition seed (`SupportDevelopmentSeedHost`) consume only `Support.Endpoints.{Seller,Admin}`
  code surfaces and `Support.Infrastructure.Development.SupportDevelopmentSeedBootstrap` — none of
  which changed name or namespace.
- `Tooba.Support.Tests` usings updated to the capability-first namespaces (and 12 accidental
  duplicate `using` lines produced by the mechanical rewrite removed; no assertion changed).
- `Tooba.Support.Tests/Architecture/SupportArchitectureGuardTests` allowlists updated **honestly**
  to the new real layout (`Domain`: `Aggregates,Entities,Enums,Events,Policies`; `Application`:
  `Tickets,Composition,Validation`; `Infrastructure`: `Persistence,Directories,Adapters,Messaging,
  DependencyInjection,Development`). No assertion was deleted or weakened.

---

## 4. Validation (focused only)

| Command | Result |
| --- | --- |
| `dotnet build Host/Tooba.Host.Tests` (builds Host + full module chain) | **0 errors** |
| `dotnet test Tooba.Support.Tests` | **13 passed / 0 failed** |
| `dotnet test Tooba.Host.Tests --filter SupportModuleAmsc001W2StructureGuardTests` | **8 passed / 0 failed** |
| `dotnet test Tooba.Host.Tests --filter SupportModuleAmsc001W1MigrateGuardTests` | **9 passed / 0 failed** (W1 guard survived the move) |
| `dotnet test Tooba.Host.Tests --filter ErrorCatalog` | pass |
| `dotnet test Tooba.Host.Tests --filter HostSupport` | pass |

New durable guard: `src/backend/Host/Tooba.Host.Tests/Architecture/SupportModuleAmsc001W2StructureGuardTests.cs`
(8 facts) locks the capability-first shallow Application layout, the absence of single-file
use-case leaf folders, the canonical `Domain/Enums` + `Persistence/Migrations` + `Development`
placement, exact path↔namespace, manifest↔disk root allowlists, the honest pre-cert manifest
record, `/Modules/Support/` solution grouping and the absence of stale copies.

### Pre-existing unrelated red (disclosed, not repaired here)

`TmarCompleteReferenceStructureGateTests.Certified_modules_satisfy_root_allowlists_and_namespace_alignment`
fails with `Expected: "Tooba.Catalog.Contracts.Cart" / Actual: "Tooba.Catalog.Contracts"` — the
documented `Tooba.Catalog.Contracts/Cart` boundary-aggregation deviation that pre-dates this task
and is already recorded as pre-existing by the AccessControl, AddressBook, CustomerProfile,
Inventory, Media, Notification, Pricing, Promotion, Returns and Story AMSC evidence trees. It
aborts on the **Catalog** project, never reaches `Support`, and Support is not a member of
`modules[]` in this wave. Also red at the W2 baseline, and out of module-local scope:

- `TmarDurableGuardTests.Recovery_current_state_is_fresh_and_machine_readable` (frozen 16-entry
  `structureLock.certifiedModules` literal vs the live repository-global list)
- `TmarDurableGuardTests.Recovery_sot_sync_001_current_checkpoint_is_unique_and_stop_is_authoritative`
  (repository-global recovery checkpoint)

Both are the same two reds already disclosed by the waves listed above; **Support appears in
neither expectation**.

---

## 5. Handoff

`Structure-State: READY_FOR_CERTIFY` → hand off to `tooba-architecture-certify` (W3).

This wave does **not** certify the module. Promotion from `preCertModules` into the certified
`modules[]` array is the exclusive authority of the W3 wave.
