# TB-TMAR-ADDRESSBOOK-AMSC-001-W2 — physical-tree-after

Production `.cs` inventory **after** W2:

```text
Tooba.AddressBook.Application/
  Addresses/                                   <- the module's single real capability (plural)
    Commands/
      CreateCustomerAddressCommand.cs
      DeleteCustomerAddressCommand.cs
      SetDefaultCustomerAddressCommand.cs
      UpdateCustomerAddressCommand.cs
    Queries/
      GetCustomerAddressQuery.cs
      ListCustomerAddressesQuery.cs
    Validators/
      CreateCustomerAddressCommandValidator.cs
      DeleteCustomerAddressCommandValidator.cs
      GetCustomerAddressQueryValidator.cs
      SetDefaultCustomerAddressCommandValidator.cs
      UpdateCustomerAddressCommandValidator.cs
  Composition/
    AddressBookOperation.cs                    <- W1 fault-to-Result seam (unchanged)
  Models/
    CustomerAddressWrite.cs                    <- shared, structural folder (unchanged)
  Ports/
    IAddressBookDirectory.cs                   <- shared, structural folder (unchanged)
  Validators/
    AddressBookFluentRules.cs                  <- shared cross-cutting rules (unchanged)
```

Untouched (already canonical):

```text
Tooba.AddressBook.Contracts/      Dtos/CustomerAddressRecord.cs, Errors/AddressBookErrorCodes.cs, Ports/IAddressBookCheckoutLookup.cs, Ports/IAddressBookCountPort.cs
Tooba.AddressBook.Domain/         Aggregates/CustomerAddress.cs
Tooba.AddressBook.Endpoints/      AddressBookEndpointModule.cs (root allowlist), Customer/{Read,Write,ActorResolver}, Errors/, Resources/
Tooba.AddressBook.Infrastructure/ Adapters/{Directory,DevelopmentSeed}, DependencyInjection/AddressBookModule.cs,
                                  Outbox/AddressBookOutboxRegistration.cs, Persistence/{DbContext,Configurations/*,Migrations/*}
```

## Tree depth

```text
Application / Addresses / Commands / <file>     depth 3 (capability -> technical axis -> file)  CANONICAL
Application / Addresses / Queries  / <file>     depth 3
Application / Addresses / Validators / <file>   depth 3
Application / Composition / <file>              depth 2
Application / Models / <file>                   depth 2
Application / Ports / <file>                    depth 2
Application / Validators / <file>               depth 2
```

No capability tree exceeds `capability -> technical axis -> files`. No ceremonial/empty folders remain
(the 11 legacy leaf directories were deleted from disk, not merely emptied).

## Classification states after W2

| State field | Value |
| --- | --- |
| Folder-Granularity-State | `PROFESSIONAL_SHALLOW` |
| Solution-Explorer-State | `CANONICAL` |
| Path-Namespace-State | `EXACT` |
| Physical-Copy-State | `CLEAN` |
| Root-Allowlist-State | `ENFORCED` |
| File-Cohesion-State | `COHESIVE` |
| Structure-State | `READY_FOR_CERTIFY` |

## Explicitly preserved (out of scope for W2)

- `Application/Validators/AddressBookFluentRules.cs` stays where it is. It is a **shared cross-cutting
  structural folder** (the same role `Validators/ContentValidationCodes.cs` plays in Content and
  `Validation/*` in AccessControl), not a use-case leaf. Renaming `Validators/` -> `Validation/` would be a
  cosmetic rename with no cohesion benefit and would churn the guard for nothing; the W0 Analyze recorded it
  as a non-blocking consistency watch (F5). W2 deliberately does **not** rename it.
- `Application/Models/CustomerAddressWrite.cs` (Application input) vs
  `Endpoints/Customer`'s `CustomerAddressWriteRequest` (HTTP body) remain two sanctioned records. W0 F7
  recorded this as a preserved invariant (owner identity stripped at the boundary,
  asserted by `AddressBookFoundationTests`). W2 does not merge them.
- `GetCustomerAddressQuery` keeps its `null`-means-missing semantics (W0 F6): a foreign address must never
  leak existence via 403. Pure structure move; behaviour identical.
