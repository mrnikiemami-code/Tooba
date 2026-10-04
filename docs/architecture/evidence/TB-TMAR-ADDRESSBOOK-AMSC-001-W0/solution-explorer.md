# TB-TMAR-ADDRESSBOOK-AMSC-001-W0 — Solution Explorer

`Solution-Explorer-State = CANONICAL`

## 1. `src/backend/Tooba.slnx`

```xml
<Folder Name="/Modules/AddressBook/">
  <Project Path="Modules/AddressBook/Tooba.AddressBook.Application/Tooba.AddressBook.Application.csproj" />
  <Project Path="Modules/AddressBook/Tooba.AddressBook.Contracts/Tooba.AddressBook.Contracts.csproj" />
  <Project Path="Modules/AddressBook/Tooba.AddressBook.Domain/Tooba.AddressBook.Domain.csproj" />
  <Project Path="Modules/AddressBook/Tooba.AddressBook.Endpoints/Tooba.AddressBook.Endpoints.csproj" />
  <Project Path="Modules/AddressBook/Tooba.AddressBook.Infrastructure/Tooba.AddressBook.Infrastructure.csproj" />
</Folder>
```

| Check | Result |
| --- | --- |
| Dedicated `/Modules/AddressBook/` Solution Folder | ✅ present |
| Production projects inside it | ✅ exactly 5 |
| Project paths resolve to real on-disk `.csproj` files | ✅ |
| Duplicate AddressBook entries elsewhere in the solution | ✅ none |
| Decorative Solution Folder disconnected from disk | ✅ none |

Guarded by `AddressBookCanonicalPresentationGuardTests.AddressBook_projects_are_grouped_under_one_solution_folder`,
which parses the XML, asserts the exact 5-entry set and asserts no AddressBook project exists under
any other Solution Folder.

## 2. Expected Solution Explorer rendering

```text
Solution 'Tooba'
└── /Modules/
    └── AddressBook/
        ├── Tooba.AddressBook.Contracts
        ├── Tooba.AddressBook.Domain
        ├── Tooba.AddressBook.Application
        ├── Tooba.AddressBook.Infrastructure
        └── Tooba.AddressBook.Endpoints
```

`Tooba.AddressBook.Tests` is **absent** (see `analyze.md` F11 — recorded, out of scope).

## 3. Solution Explorer folder rendering inside `Tooba.AddressBook.Application`

The solution folder grouping is canonical, but the *project-internal* tree that Visual Studio renders
is the `TECHNICAL_AXIS_FIRST` shape documented in `folder-granularity.md`:

```text
Tooba.AddressBook.Application
├── Commands
│   ├── CreateCustomerAddress      ← 1 file
│   ├── DeleteCustomerAddress      ← 1 file
│   ├── SetDefaultCustomerAddress  ← 1 file
│   └── UpdateCustomerAddress      ← 1 file
├── Models                          ← 1 file
├── Ports                           ← 1 file
├── Queries
│   ├── GetCustomerAddress         ← 1 file
│   └── ListCustomerAddresses      ← 1 file
└── Validators
    ├── AddressBookFluentRules.cs
    ├── CreateCustomerAddress      ← 1 file
    ├── DeleteCustomerAddress      ← 1 file
    ├── GetCustomerAddress         ← 1 file
    ├── SetDefaultCustomerAddress  ← 1 file
    └── UpdateCustomerAddress      ← 1 file
```

W2 must flatten this to a capability-first shallow tree.

## 4. Other project trees (already professional)

| Project | Rendering | Verdict |
| --- | --- | --- |
| `Tooba.AddressBook.Contracts` | `Dtos/`, `Errors/`, `Ports/` | canonical |
| `Tooba.AddressBook.Domain` | `Aggregates/CustomerAddress.cs` | canonical |
| `Tooba.AddressBook.Infrastructure` | `Adapters/`, `DependencyInjection/`, `Outbox/`, `Persistence/{Configurations,Migrations}` | canonical |
| `Tooba.AddressBook.Endpoints` | root `AddressBookEndpointModule.cs` (allowlisted) + `Customer/`, `Errors/`, `Resources/` | canonical |

## 5. Manifest reconciliation

`tmar-module-structure-manifests.json` declares AddressBook `structureCertified: true` with
`projects[]` entries per project. Those entries are **solution-grouping consistent** but the
`Tooba.AddressBook.Application` entry (`rootAllowlist: []`, justification citing
`Commands/<UseCase>, Queries/<UseCase>`) documents the non-canonical tree as accepted — see
`manifest-structure.md`. That justification must be corrected in W2/W3.
