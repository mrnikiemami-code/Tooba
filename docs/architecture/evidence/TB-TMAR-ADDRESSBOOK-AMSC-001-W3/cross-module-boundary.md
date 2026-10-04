# TB-TMAR-ADDRESSBOOK-AMSC-001-W3 — cross-module-boundary

`CrossModuleBoundaryState = CONTRACTS_ONLY`. `ForeignAppInfraDomainCoupling = ZERO`.
`CrossModuleJoinState = ZERO`. `CrossModulePersistenceState = ZERO`.

## Outgoing edges (AddressBook → other modules)

### Project references

| From | To | Kind | Verdict |
| --- | --- | --- | --- |
| `Tooba.AddressBook.Contracts` | `Tooba.BuildingBlocks` | platform | LEGAL |
| `Tooba.AddressBook.Domain` | `Tooba.BuildingBlocks` | platform | LEGAL |
| `Tooba.AddressBook.Domain` | `Tooba.AddressBook.Contracts` | same-module | LEGAL |
| `Tooba.AddressBook.Application` | `Tooba.BuildingBlocks` | platform | LEGAL |
| `Tooba.AddressBook.Application` | `Tooba.AddressBook.Domain` | same-module | LEGAL |
| `Tooba.AddressBook.Application` | `Tooba.AddressBook.Contracts` | same-module | LEGAL |
| `Tooba.AddressBook.Infrastructure` | `Tooba.AddressBook.Application` | same-module | LEGAL |
| `Tooba.AddressBook.Infrastructure` | `Tooba.Order.Contracts` | **foreign boundary contract** | LEGAL |
| `Tooba.AddressBook.Infrastructure` | `Tooba.ModuleContracts` | platform | LEGAL |
| `Tooba.AddressBook.Infrastructure` | `Tooba.Persistence` | platform | LEGAL |
| `Tooba.AddressBook.Endpoints` | `Tooba.AddressBook.Application` | same-module | LEGAL |
| `Tooba.AddressBook.Endpoints` | `Tooba.BuildingBlocks` | platform | LEGAL |
| `Tooba.AddressBook.Endpoints` | `Tooba.Order.Contracts` | **foreign boundary contract** | LEGAL |

### Namespace usings of foreign modules

| File | Using | Purpose |
| --- | --- | --- |
| `Endpoints/Customer/AddressBookCustomerActorResolver.cs` | `Tooba.Order.Contracts.Fulfillment` | stable `StorefrontGuestActor.ActorId` contract constant for the trusted guest actor |
| `Infrastructure/Adapters/AddressBookDevelopmentSeed.cs` | `Tooba.Order.Contracts.Fulfillment` | same stable constant, development seed |

Platform usings: `Tooba.BuildingBlocks*`, `Tooba.ModuleContracts`, `Tooba.Persistence`.

### Forbidden references — verified absent

| Forbidden | Count |
| --- | --- |
| `Tooba.Order.Application` / `.Domain` / `.Infrastructure` | 0 |
| `Tooba.CustomerProfile.Application` / `.Domain` / `.Infrastructure` | 0 |
| `Tooba.Catalog.Application` / `.Domain` / `.Infrastructure` | 0 |
| `Tooba.Party.Application` / `.Domain` / `.Infrastructure` | 0 |
| `Tooba.Identity.Application` / `.Domain` / `.Infrastructure` | 0 |
| `Tooba.Media.Application` / `.Domain` / `.Infrastructure` | 0 |
| any foreign `DbContext` / `DbSet` | 0 |
| any foreign repository implementation | 0 |
| any cross-module SQL/EF join | 0 |
| direct table/schema reach-through | 0 |
| shared mutable aggregate | 0 |

Asserted by `AddressBookModuleAmsc001W3CertGuardTests.AddressBook_has_no_foreign_module_project_or_namespace_dependency`
(csproj + `using` scan) — PASS. The only allowed foreign edge (`Tooba.Order.Contracts.Fulfillment`) is
positively asserted so a future silent removal also fails the guard.

## Incoming edges (other modules → AddressBook)

| Consumer | Consumes | Kind |
| --- | --- | --- |
| `Order.Application` | `IAddressBookCheckoutLookup`, `CustomerAddressRecord` | Contracts boundary |
| `CustomerProfile.Application` | `IAddressBookCountPort` | Contracts boundary |

No consumer reaches into `Tooba.AddressBook.Application`, `.Domain` or `.Infrastructure`. This is what makes
the module extractable: the Contracts assembly is the whole public surface.

## No cross-module join proof

`AddressBookDirectory` (the only EF-touching adapter) references exactly one `DbSet` from exactly one
`DbContext`:

```text
AddressBookDbContext.Addresses   (schema: address_book)
```

There is no `Include`, no `Join`, no `FromSql` against a foreign table, no navigation to a foreign entity,
and no foreign `DbContext` injection anywhere in the module. The Order checkout lookup reads only the
module's own `Addresses` set. `CrossModuleJoinState = ZERO`.

## Legal boundary surface (Contracts) — semantic ownership audit

| Type | Location | Why it belongs there |
| --- | --- | --- |
| `CustomerAddressRecord` | `Contracts/Dtos` | cross-module read snapshot consumed by `Order.Application` |
| `IAddressBookCheckoutLookup` | `Contracts/Ports` | narrow Order checkout read port |
| `IAddressBookCountPort` | `Contracts/Ports` | narrow CustomerProfile dashboard count port |
| `AddressBookErrorCodes` | `Contracts/Errors` | module-owned stable codes (also consumed by the Domain aggregate) |
| `IAddressBookDirectory` | `Application/Ports` | Application-internal write/list surface — correctly **not** in Contracts |
| `CustomerAddressWrite` | `Application/Models` | Application-internal write input — correctly **not** in Contracts |

No mixed `*Contracts.cs` Application dump exists. Contracts holds only stable boundary types.
