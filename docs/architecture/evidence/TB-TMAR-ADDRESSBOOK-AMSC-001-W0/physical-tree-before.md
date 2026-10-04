# TB-TMAR-ADDRESSBOOK-AMSC-001-W0 — Physical Tree (Before)

Root: `src/backend/Modules/AddressBook/`
`bin/` and `obj/` omitted. `[D]` = directory, otherwise file.

```text
[D] Tooba.AddressBook.Application
    Tooba.AddressBook.Application/Tooba.AddressBook.Application.csproj
[D] Tooba.AddressBook.Application/Commands
[D] Tooba.AddressBook.Application/Commands/CreateCustomerAddress
    Tooba.AddressBook.Application/Commands/CreateCustomerAddress/CreateCustomerAddressCommand.cs
[D] Tooba.AddressBook.Application/Commands/DeleteCustomerAddress
    Tooba.AddressBook.Application/Commands/DeleteCustomerAddress/DeleteCustomerAddressCommand.cs
[D] Tooba.AddressBook.Application/Commands/SetDefaultCustomerAddress
    Tooba.AddressBook.Application/Commands/SetDefaultCustomerAddress/SetDefaultCustomerAddressCommand.cs
[D] Tooba.AddressBook.Application/Commands/UpdateCustomerAddress
    Tooba.AddressBook.Application/Commands/UpdateCustomerAddress/UpdateCustomerAddressCommand.cs
[D] Tooba.AddressBook.Application/Models
    Tooba.AddressBook.Application/Models/CustomerAddressWrite.cs
[D] Tooba.AddressBook.Application/Ports
    Tooba.AddressBook.Application/Ports/IAddressBookDirectory.cs
[D] Tooba.AddressBook.Application/Queries
[D] Tooba.AddressBook.Application/Queries/GetCustomerAddress
    Tooba.AddressBook.Application/Queries/GetCustomerAddress/GetCustomerAddressQuery.cs
[D] Tooba.AddressBook.Application/Queries/ListCustomerAddresses
    Tooba.AddressBook.Application/Queries/ListCustomerAddresses/ListCustomerAddressesQuery.cs
[D] Tooba.AddressBook.Application/Validators
    Tooba.AddressBook.Application/Validators/AddressBookFluentRules.cs
[D] Tooba.AddressBook.Application/Validators/CreateCustomerAddress
    Tooba.AddressBook.Application/Validators/CreateCustomerAddress/CreateCustomerAddressCommandValidator.cs
[D] Tooba.AddressBook.Application/Validators/DeleteCustomerAddress
    Tooba.AddressBook.Application/Validators/DeleteCustomerAddress/DeleteCustomerAddressCommandValidator.cs
[D] Tooba.AddressBook.Application/Validators/GetCustomerAddress
    Tooba.AddressBook.Application/Validators/GetCustomerAddress/GetCustomerAddressQueryValidator.cs
[D] Tooba.AddressBook.Application/Validators/SetDefaultCustomerAddress
    Tooba.AddressBook.Application/Validators/SetDefaultCustomerAddress/SetDefaultCustomerAddressCommandValidator.cs
[D] Tooba.AddressBook.Application/Validators/UpdateCustomerAddress
    Tooba.AddressBook.Application/Validators/UpdateCustomerAddress/UpdateCustomerAddressCommandValidator.cs

[D] Tooba.AddressBook.Contracts
    Tooba.AddressBook.Contracts/Tooba.AddressBook.Contracts.csproj
[D] Tooba.AddressBook.Contracts/Dtos
    Tooba.AddressBook.Contracts/Dtos/CustomerAddressRecord.cs
[D] Tooba.AddressBook.Contracts/Errors
    Tooba.AddressBook.Contracts/Errors/AddressBookErrorCodes.cs
[D] Tooba.AddressBook.Contracts/Ports
    Tooba.AddressBook.Contracts/Ports/IAddressBookCheckoutLookup.cs
    Tooba.AddressBook.Contracts/Ports/IAddressBookCountPort.cs

[D] Tooba.AddressBook.Domain
    Tooba.AddressBook.Domain/Tooba.AddressBook.Domain.csproj
[D] Tooba.AddressBook.Domain/Aggregates
    Tooba.AddressBook.Domain/Aggregates/CustomerAddress.cs

[D] Tooba.AddressBook.Endpoints
    Tooba.AddressBook.Endpoints/AddressBookEndpointModule.cs
    Tooba.AddressBook.Endpoints/Tooba.AddressBook.Endpoints.csproj
[D] Tooba.AddressBook.Endpoints/Customer
    Tooba.AddressBook.Endpoints/Customer/AddressBookCustomerActorResolver.cs
    Tooba.AddressBook.Endpoints/Customer/AddressBookCustomerReadEndpoints.cs
    Tooba.AddressBook.Endpoints/Customer/AddressBookCustomerWriteEndpoints.cs
[D] Tooba.AddressBook.Endpoints/Errors
    Tooba.AddressBook.Endpoints/Errors/AddressBookErrorCatalogContributor.cs
[D] Tooba.AddressBook.Endpoints/Resources
    Tooba.AddressBook.Endpoints/Resources/AddressBookErrorResources.cs
    Tooba.AddressBook.Endpoints/Resources/AddressBookErrors.fa.resx
    Tooba.AddressBook.Endpoints/Resources/AddressBookErrors.resx

[D] Tooba.AddressBook.Infrastructure
    Tooba.AddressBook.Infrastructure/Tooba.AddressBook.Infrastructure.csproj
[D] Tooba.AddressBook.Infrastructure/Adapters
    Tooba.AddressBook.Infrastructure/Adapters/AddressBookDevelopmentSeed.cs
    Tooba.AddressBook.Infrastructure/Adapters/AddressBookDirectory.cs
[D] Tooba.AddressBook.Infrastructure/DependencyInjection
    Tooba.AddressBook.Infrastructure/DependencyInjection/AddressBookModule.cs
[D] Tooba.AddressBook.Infrastructure/Outbox
    Tooba.AddressBook.Infrastructure/Outbox/AddressBookOutboxRegistration.cs
[D] Tooba.AddressBook.Infrastructure/Persistence
    Tooba.AddressBook.Infrastructure/Persistence/AddressBookDbContext.cs
[D] Tooba.AddressBook.Infrastructure/Persistence/Configurations
    Tooba.AddressBook.Infrastructure/Persistence/Configurations/CustomerAddressConfiguration.cs
[D] Tooba.AddressBook.Infrastructure/Persistence/Migrations
    Tooba.AddressBook.Infrastructure/Persistence/Migrations/20260825171858_InitialAddressBook.cs
    Tooba.AddressBook.Infrastructure/Persistence/Migrations/20260825171858_InitialAddressBook.Designer.cs
    Tooba.AddressBook.Infrastructure/Persistence/Migrations/20260913180000_AddRecipientNameParts.cs
    Tooba.AddressBook.Infrastructure/Persistence/Migrations/AddressBookDbContextModelSnapshot.cs
```

## Counts

| Project | Hand-written production `.cs` | EF-generated | Root `.cs` |
| --- | --- | --- | --- |
| `Tooba.AddressBook.Contracts` | 4 | 0 | 0 |
| `Tooba.AddressBook.Domain` | 1 | 0 | 0 |
| `Tooba.AddressBook.Application` | 13 | 0 | 0 |
| `Tooba.AddressBook.Infrastructure` | 5 | 4 | 0 |
| `Tooba.AddressBook.Endpoints` | 6 | 0 | 1 (`AddressBookEndpointModule.cs`, allowlisted) |
| **Total** | **29** | **4** | **1** |

## Structural classification

| State | Value |
| --- | --- |
| `Folder-Granularity-State` | `TECHNICAL_AXIS_FIRST` (+ 11 unjustified single-file leaves) |
| `Solution-Explorer-State` | `CANONICAL` |
| `Path-Namespace-State` | `EXACT` |
| `Physical-Copy-State` | `CLEAN` |
| `Root-Allowlist-State` | `ENFORCED` |
| `File-Cohesion-State` | `COHESIVE` |
