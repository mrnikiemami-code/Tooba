# Folder granularity (skill §7–§11)

`Folder-Granularity-State = PROFESSIONAL_SHALLOW`

## Rule application

| Skill rule | Application to Settlement | Verdict |
|---|---|---|
| §7 capability is the primary axis | Application is `Payouts/` (the module's single real capability) with `Composition/` + `Validation/` as genuinely shared leaves | PASS |
| §7 technical axes are secondary | `Commands`, `Queries`, `Models`, `Ports` live **under** `Payouts/` | PASS |
| §8 single-file use-case leaf folder | `Payouts/Commands` = 3 files flat, `Payouts/Queries` = 8 files flat, zero subfolders | PASS |
| §8 count source files, not types | 3 command files, 8 query files — no wrapper folder holds one `.cs` | PASS |
| §9 per-use-case exception | not needed: no use case has multiple cohesive production files that would benefit from isolation | n/a |
| §10 technical-axis-first detection | no `Application/Commands/<UseCase>`, `Application/Queries/<UseCase>` or `Application/Validators/<UseCase>` exists | PASS |
| §11 depth/explosion | maximum depth is 3 (`Payouts/Commands/<file>.cs`); no empty ceremonial folder | PASS |

## Measured shape

```text
Application/                     depth 1  (3 folders)
Application/Payouts/             depth 2  (4 folders: Commands, Models, Ports, Queries)
Application/Payouts/Commands/    depth 3  (0 folders, 3 files)
Application/Payouts/Queries/     depth 3  (0 folders, 8 files)
Application/Payouts/Models/      depth 3  (0 folders, 2 files)
Application/Payouts/Ports/       depth 3  (0 folders, 5 files)
Application/Composition/         depth 2  (0 folders, 1 file)
Application/Validation/          depth 2  (0 folders, 2 files)
```

## Single-capability justification (skill §7 last paragraph)

Settlement has exactly **one** business capability — seller/admin payout & settlement bookkeeping — so
`Payouts/` is the capability root and the module does not need a second capability folder. The two
extra leaves are justified, not ceremonial:

- `Composition/` — the single cross-cutting typed-fault seam (`SettlementOperation`) consumed by all
  three command handlers. A leaf that is shared by a whole capability belongs above it.
- `Validation/` — transport validators + validation codes for the four `VALIDATOR_REQUIRED` requests,
  shared by both audiences. The audience-first `Validators/{Admin,Seller}` tree was deliberately
  retired (W0 §14 handed the decision to W2 and recommended the flat leaf), matching the certified
  Returns / Promotion / AddressBook precedent.

## Retired trees (now forbidden)

```text
Application/Commands/<UseCase>/...      FORBIDDEN
Application/Queries/<UseCase>/...       FORBIDDEN
Application/Validators/<UseCase>/...    FORBIDDEN
Application/{Models,Ports,Errors}/...   FORBIDDEN
Application/Handlers/...                FORBIDDEN
Application/Requests/...                FORBIDDEN
```

These are locked by `forbiddenTopLevelFolders` in
`docs/architecture/tmar-module-structure-manifests.json` and by the durable guard test
`SettlementModuleAmsc001W2StructureGuardTests.Application_is_capability_first_shallow_without_technical_axis_roots`.

## Non-Application layers

| Layer | Shape | Verdict |
|---|---|---|
| Contracts | `Errors/`, `Events/`, `History/`, `Operations/`, `Resources/` | capability-foldered boundary vocabulary; no root `.cs` |
| Domain | `Aggregates/`, `Entities/`, `Events/`, `ValueObjects/` | module-established pattern; no root god-file |
| Infrastructure | `Adapters/`, `Bridges/`, `DependencyInjection/`, `Directories/`, `Errors/`, `Gateways/`, `Handlers/`, `Messaging/`, `Observability/`, `Persistence/`(+`Migrations/`), `Queries/` | capability/integration folders, persistence foldered |
| Endpoints | `Admin/`, `Seller/` + composition entry at root | audience capability folders |
