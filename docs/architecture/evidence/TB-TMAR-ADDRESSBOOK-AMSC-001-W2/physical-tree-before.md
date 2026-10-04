# TB-TMAR-ADDRESSBOOK-AMSC-001-W2 — physical-tree-before

- Task: `TB-TMAR-ADDRESSBOOK-AMSC-001-W2`
- Skill: `.cursor/skills/tooba-architecture-structure/SKILL.md`
- Branch: `main`
- HEAD at W2 start: `3080d3e2eda5b30e65b501ca74873cdbd029365d` (== `origin/main`, W1 commit)
- Target: `src/backend/Modules/AddressBook/Tooba.AddressBook.*`

Production `.cs` inventory **before** W2 (excluding `bin/`, `obj/`, EF `Migrations/`), Application project only
(the other four projects were already canonical and are not touched by W2):

```text
Tooba.AddressBook.Application/
  Commands/
    CreateCustomerAddress/CreateCustomerAddressCommand.cs        <- 1 source file  OVER_FOLDERED
    DeleteCustomerAddress/DeleteCustomerAddressCommand.cs        <- 1 source file  OVER_FOLDERED
    SetDefaultCustomerAddress/SetDefaultCustomerAddressCommand.cs <- 1 source file OVER_FOLDERED
    UpdateCustomerAddress/UpdateCustomerAddressCommand.cs        <- 1 source file  OVER_FOLDERED
  Queries/
    GetCustomerAddress/GetCustomerAddressQuery.cs                <- 1 source file  OVER_FOLDERED
    ListCustomerAddresses/ListCustomerAddressesQuery.cs          <- 1 source file  OVER_FOLDERED
  Validators/
    AddressBookFluentRules.cs                                    (shared, 1 file, structural folder — legal)
    CreateCustomerAddress/CreateCustomerAddressCommandValidator.cs        <- 1 source file OVER_FOLDERED
    DeleteCustomerAddress/DeleteCustomerAddressCommandValidator.cs        <- 1 source file OVER_FOLDERED
    GetCustomerAddress/GetCustomerAddressQueryValidator.cs                <- 1 source file OVER_FOLDERED
    SetDefaultCustomerAddress/SetDefaultCustomerAddressCommandValidator.cs <- 1 source file OVER_FOLDERED
    UpdateCustomerAddress/UpdateCustomerAddressCommandValidator.cs        <- 1 source file OVER_FOLDERED
  Composition/AddressBookOperation.cs                            (W1, canonical)
  Models/CustomerAddressWrite.cs                                 (shared, 1 file, structural folder — legal)
  Ports/IAddressBookDirectory.cs                                 (shared, 1 file, structural folder — legal)
```

Other projects (unchanged by W2, already canonical):

```text
Tooba.AddressBook.Contracts/    Dtos/CustomerAddressRecord.cs, Errors/AddressBookErrorCodes.cs, Ports/*.cs
Tooba.AddressBook.Domain/       Aggregates/CustomerAddress.cs
Tooba.AddressBook.Endpoints/    AddressBookEndpointModule.cs (root allowlist), Customer/*, Errors/*, Resources/*
Tooba.AddressBook.Infrastructure/ Adapters/*, DependencyInjection/*, Outbox/*, Persistence/*, Persistence/Migrations/*, Persistence/Configurations/*
```

## Classification states before W2

| State field | Value |
| --- | --- |
| Folder-Granularity-State | `TECHNICAL_AXIS_FIRST` (+ 11 `OVER_FOLDERED` single-file use-case leaves) |
| Solution-Explorer-State | `CANONICAL` (`/Modules/AddressBook/`, 5/5 projects) |
| Path-Namespace-State | `EXACT` (exactness held even though the shape was non-canonical) |
| Physical-Copy-State | `CLEAN` |
| Root-Allowlist-State | `ENFORCED` |
| File-Cohesion-State | `COHESIVE` (largest hand-written file `AddressBookDirectory.cs`, ~220 LOC) |
| Structure-State | `REPAIR_REQUIRED` |

## Certification drift recorded

`tmar-module-structure-manifests.json` (module `AddressBook`, project `Tooba.AddressBook.Application`)
documented the non-canonical shape as accepted:

> "AddressBook.Application intentionally has no root .cs file: `Commands/<UseCase>`, `Queries/<UseCase>`, Models, Ports, …"

That justification is the drift itself and is corrected in W2 (see `manifest-structure.md`).
