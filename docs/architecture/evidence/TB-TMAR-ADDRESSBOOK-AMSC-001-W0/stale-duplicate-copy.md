# TB-TMAR-ADDRESSBOOK-AMSC-001-W0 — Stale / Duplicate Copy

`Physical-Copy-State = CLEAN`

## 1. Duplicate type-name scan

Scope: all `.cs` under `src/backend/Modules/AddressBook`, excluding `bin/`, `obj/`,
`Persistence/Migrations/`, and `*ModelSnapshot.cs` (EF-generated, exempt).

Result: **no duplicate file-name / type-name pair.**

The only repeated file base names are the EF-generated ones
(`20260825171858_InitialAddressBook.Designer.cs` alongside
`20260825171858_InitialAddressBook.cs`), which are intentionally exempt.

Guarded by
`AddressBookPhysicalStructureGuardTests.AddressBook_has_no_stale_or_duplicate_physical_type_copies`.

## 2. Stale-path scan (previous realignment residue)

The module previously moved `Domain/CustomerAddress.cs`, `Contracts/Customer/`,
`Application/Customer/`, `Infrastructure/Directories/`, `Infrastructure/Development/` and
`Infrastructure/Migrations/`. All of them are absent:

| Legacy path | Present? |
| --- | --- |
| `Tooba.AddressBook.Domain/CustomerAddress.cs` | ❌ absent |
| `Tooba.AddressBook.Contracts/Customer/` | ❌ absent |
| `Tooba.AddressBook.Application/Customer/` | ❌ absent |
| `Tooba.AddressBook.Infrastructure/Directories/` | ❌ absent |
| `Tooba.AddressBook.Infrastructure/Development/` | ❌ absent |
| `Tooba.AddressBook.Infrastructure/Migrations/` | ❌ absent |

Guarded by `AddressBookPhysicalStructureGuardTests.AddressBook_domain_aggregate_and_contracts_ports_are_in_offer_style_locations`
and `AddressBookValidatorCoverageGuardTests.AddressBook_root_capability_files_were_evacuated`.

## 3. Solution-entry staleness

Every `<Project Path="…">` under `/Modules/AddressBook/` resolves to an existing `.csproj` on disk.
No AddressBook project is missing from the solution; no entry points at a deleted path.

## 4. Host residue staleness

| Legacy Host artifact | Present? |
| --- | --- |
| `src/backend/Host/Tooba.Host/AddressBook/AddressBookEndpoints.cs` | ❌ absent |
| `src/backend/Host/Tooba.Host/AddressBook/AddressBookDevelopmentSeed.cs` | ❌ absent |

The Host `AddressBook/` folder no longer exists. `hostAddressBookResidue: []` is recorded in SoT
(`tmar-current-state.json` lines 1625, 1854, 1890).

Guarded by `AddressBookFoundationTests.Endpoint_uses_session_and_rejects_missing_production_actor`
(`Assert.False(File.Exists(…/Host/Tooba.Host/AddressBook/AddressBookEndpoints.cs))`).

## 5. Working-tree stray artifact (not AddressBook)

`docs/architecture/evidence/TB-TMAR-ORDER-AMC-001-W5-R1/worker-result.txt` is untracked and belongs
to the earlier Order run. It will **not** be committed by this AMSC run.

The untracked `src/backend/.vs/…` and `bin/…` artifacts are IDE/build output and are also excluded.
