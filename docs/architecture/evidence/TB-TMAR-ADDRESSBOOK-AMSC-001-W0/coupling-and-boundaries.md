# TB-TMAR-ADDRESSBOOK-AMSC-001-W0 — Coupling and Boundaries

`Cross-Module-Coupling-State = LEGAL_CONTRACTS_ONLY`
`Cross-Module-Join-State = NONE`
`Persistence-Ownership-State = CORRECT`

## 1. ProjectReference inventory (AddressBook → others)

| Project | ProjectReference | Verdict |
| --- | --- | --- |
| `Tooba.AddressBook.Contracts` | *(none)* | CLEAN — pure boundary assembly |
| `Tooba.AddressBook.Domain` | `Tooba.BuildingBlocks` | platform seam — legal |
| `Tooba.AddressBook.Application` | `Tooba.BuildingBlocks`, `Tooba.AddressBook.Domain`, `Tooba.AddressBook.Contracts` | CLEAN — no foreign module |
| `Tooba.AddressBook.Endpoints` | `Tooba.AddressBook.Application`, `Tooba.BuildingBlocks`, `Tooba.Order.Contracts` | `Order.Contracts` is legal boundary |
| `Tooba.AddressBook.Infrastructure` | `Tooba.AddressBook.Application`, `Tooba.Order.Contracts`, `Tooba.ModuleContracts`, `Tooba.Persistence` | `Order.Contracts` legal; rest are platform seams |

**Zero** foreign `*.Application`, `*.Infrastructure`, `*.Domain` project references.

## 2. Foreign `using` inventory (AddressBook → others)

Exhaustive scan of `using Tooba.<not AddressBook|BuildingBlocks|ModuleContracts|Persistence>.`:

```text
using Tooba.Order.Contracts.Fulfillment;     (2 sites)
```

| Site | Symbol used | Verdict |
| --- | --- | --- |
| `Tooba.AddressBook.Endpoints/Customer/AddressBookCustomerActorResolver.cs` | `StorefrontGuestActor.ActorId` | legal stable contract constant |
| `Tooba.AddressBook.Infrastructure/Adapters/AddressBookDevelopmentSeed.cs` | `StorefrontGuestActor.ActorId` | legal stable contract constant |

No foreign Application/Domain/Infrastructure type is referenced anywhere in the module.

## 3. Forbidden-coupling scan results

| Pattern | Hits |
| --- | --- |
| `OtherModule.Application` | **0** |
| `OtherModule.Infrastructure` | **0** |
| `OtherModule.Domain` | **0** |
| foreign `DbContext` / `DbSet` | **0** |
| foreign repository/store/directory implementation | **0** |
| EF navigation crossing a module boundary | **0** (the aggregate has no navigation properties at all) |
| cross-module SQL / EF join | **0** |
| shared mutable entity | **0** |
| Host-owned business policy | **0** |
| Host-owned persistence policy | **0** |
| endpoint → infrastructure direct call | **0** |

## 4. Reverse edge inventory (others → AddressBook)

| Consumer | Reference | Verdict |
| --- | --- | --- |
| `Tooba.Order.Application` | csproj → `Tooba.AddressBook.Contracts`; usings `Contracts.Ports.IAddressBookCheckoutLookup`, `Contracts.Dtos.CustomerAddressRecord` | legal — narrow read port |
| `Tooba.CustomerProfile.Application` | csproj → `Tooba.AddressBook.Contracts`; using `Contracts.Ports.IAddressBookCountPort` | legal — narrow count port |
| `Tooba.Host` | csproj → Application, Infrastructure, Endpoints; composition calls | legal composition root |
| `Tooba.Host.Tests` | csproj → `Tooba.AddressBook.Endpoints`; behaviour + architecture guards | legal |
| `Tooba.MigrationRunner` | `AddressBookDbContext` descriptor | legal platform seam |
| `Tooba.Persistence` | `ModuleSchemaMigrator.ModuleSchemaMigrationOrder.AddressBook = 18` | legal platform seam |

`Order.Tests/Architecture/OrderStorefrontArchitectureGuardTests` already enforces the boundary from
the consumer side:

```text
Assert.DoesNotContain("Tooba.AddressBook.Application", …)
Assert.DoesNotContain("Tooba.AddressBook.Infrastructure", …)
Assert.Contains("AddressBook.Contracts", csproj, …)
```

## 5. Cross-module join inventory

**NONE.**

`AddressBookDirectory` queries only `_db.Addresses` (its own `address_book.customer_addresses`
table). The only `ExecuteUpdateAsync` and the only tracked-entity sweep in the module both filter on
`OwnerUserId` against the module's own set.

## 6. Persistence ownership

| Check | Result |
| --- | --- |
| Module DbContext | `AddressBookDbContext` only |
| Schema | `address_book` (module-owned) |
| Tables | `customer_addresses`, outbox table |
| Migrations | 2, module-owned, under `Persistence/Migrations/` |
| Foreign FK / foreign schema read | none |
| DbContext reachable from Application/Endpoints | no |
| `ARCH-DATA-001` | intact |

## 7. Host authority classification

| Host reference | Category |
| --- | --- |
| `Composition/ToobaModuleComposition.cs` → `new AddressBookModule()` | `ALLOWED_COMPOSITION_ROOT` |
| `Program.cs` → `AddAddressBookEndpointPresentation()` | `ALLOWED_COMPOSITION_ROOT` |
| `Program.cs` → `MapAddressBookModuleEndpoints()` | `ALLOWED_COMPOSITION_ROOT` |
| `Program.cs` → CQRS assembly scan `typeof(IAddressBookDirectory).Assembly` | `ALLOWED_COMPOSITION_ROOT` |
| `Program.cs` → `IAddressBookCheckoutLookup` → `IAddressBookDirectory` alias registration | `ALLOWED_CONTRACT_CONSUMPTION` |
| `Development/DevelopmentSchemaMigrator.cs` → `AddressBookDevelopmentSeed.ApplyAsync` | `ALLOWED_COMPOSITION_ROOT` (development seed invocation) |
| `Tooba.MigrationRunner/ModuleMigrationRegistry.cs` → `Descriptor<AddressBookDbContext>` | `ALLOWED_COMPOSITION_ROOT` |

| Illegal category | Count |
| --- | --- |
| `ILLEGAL_BUSINESS_AUTHORITY` | **0** |
| `ILLEGAL_PERSISTENCE_AUTHORITY` | **0** |
| `ILLEGAL_ENDPOINT_OWNERSHIP` | **0** |
| `STRUCTURAL_DEBT_ONLY` | **0** |

## 8. Microservice extractability

AddressBook lifts with:

- `Tooba.BuildingBlocks` (platform)
- `Tooba.Persistence` + `Tooba.ModuleContracts` (platform)
- `Tooba.Order.Contracts` (one constant: `StorefrontGuestActor.ActorId`)

No foreign persistence, no foreign domain type, no shared mutable state, no Host authority. The only
shared-type coupling is a **single immutable `Guid` constant on a Contracts type** — a legal boundary
dependency, and the smallest possible one.

`microserviceExtractable = true`
