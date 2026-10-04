# TB-TMAR-ADDRESSBOOK-AMSC-001-W2 — solution-explorer

`Solution-Explorer-State = CANONICAL` (verified, **not changed** by W2).

## Evidence

`src/backend/Tooba.slnx`:

```xml
  <Folder Name="/Modules/AddressBook/">
    <Project Path="Modules/AddressBook/Tooba.AddressBook.Application/Tooba.AddressBook.Application.csproj" />
    <Project Path="Modules/AddressBook/Tooba.AddressBook.Contracts/Tooba.AddressBook.Contracts.csproj" />
    <Project Path="Modules/AddressBook/Tooba.AddressBook.Domain/Tooba.AddressBook.Domain.csproj" />
    <Project Path="Modules/AddressBook/Tooba.AddressBook.Endpoints/Tooba.AddressBook.Endpoints.csproj" />
    <Project Path="Modules/AddressBook/Tooba.AddressBook.Infrastructure/Tooba.AddressBook.Infrastructure.csproj" />
```

Checks:

| Check | Result |
| --- | --- |
| Exactly one `/Modules/AddressBook/` Solution Folder | PASS (line 119) |
| All 5 on-disk projects are listed | PASS (5/5) |
| No project listed that is absent on disk (stale entry) | PASS |
| No AddressBook project left at solution root or under a wrong folder | PASS |
| Assemblies renamed for visuals | NONE |
| `.csproj` files moved for visuals | NONE |
| Decorative Solution Folder disconnected from disk | NONE |

The W2 repair is a **disk-only** move inside the Application project; the Application `.csproj` is
SDK-style globbing, so no project-file edit was needed and no Solution Explorer entry changed. Visual Studio
Solution Explorer automatically renders the new `Addresses` capability node (and stops showing the removed
`Commands`/`Queries`/per-use-case nodes), because `.slnx` groups by project and the in-project tree mirrors
disk.

## Structural view after W2 (Visual Studio)

```text
/Modules/AddressBook/
  Tooba.AddressBook.Application
    Addresses
      Commands        (4 commands, each with its handler)
      Queries         (2 queries, each with its handler)
      Validators      (5 transport validators)
    Composition       (AddressBookOperation)
    Models            (CustomerAddressWrite)
    Ports             (IAddressBookDirectory)
    Validators        (AddressBookFluentRules)
  Tooba.AddressBook.Contracts
    Dtos / Errors / Ports
  Tooba.AddressBook.Domain
    Aggregates
  Tooba.AddressBook.Endpoints
    Customer / Errors / Resources  (+ AddressBookEndpointModule.cs at root)
  Tooba.AddressBook.Infrastructure
    Adapters / DependencyInjection / Outbox / Persistence (Configurations, Migrations)
```

Depth is `project → capability → technical axis → file` at its deepest. No over-nesting, no empty
ceremonial folders.
