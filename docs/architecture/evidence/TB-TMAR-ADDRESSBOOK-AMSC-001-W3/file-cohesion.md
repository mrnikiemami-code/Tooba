# TB-TMAR-ADDRESSBOOK-AMSC-001-W3 — file-cohesion

`File-Cohesion-State = COHESIVE`. No baseline entry. No god-file. No over-split.

## Size inventory (hand-written production sources)

| File | LOC (approx.) | Responsibility | Verdict |
| --- | --- | --- | --- |
| `Infrastructure/Adapters/AddressBookDirectory.cs` | ~220 | EF adapter: create/update/delete/set-default/list/get + owner check + recipient composition | COHESIVE (well under the 800 threshold) |
| `Domain/Aggregates/CustomerAddress.cs` | ~230 | `CustomerAddress` aggregate + invariants | COHESIVE |
| `Endpoints/Customer/AddressBookCustomerWriteEndpoints.cs` | ~140 | 4 write routes + body→Application mapping | COHESIVE |
| `Endpoints/Customer/AddressBookCustomerReadEndpoints.cs` | ~60 | 2 read routes | COHESIVE |
| `Application/Composition/AddressBookOperation.cs` | ~40 | Fault→Result seam | COHESIVE |
| `Application/Validators/AddressBookFluentRules.cs` | ~50 | Validation codes + Fluent helpers | COHESIVE |
| `Endpoints/Errors/AddressBookErrorCatalogContributor.cs` | ~45 | 12 descriptors | COHESIVE |
| `Endpoints/Resources/AddressBookErrorResources.cs` | ~15 | `IErrorResourceSet` seam | COHESIVE |
| `Endpoints/Customer/AddressBookCustomerActorResolver.cs` | ~30 | Trusted actor seam | COHESIVE |
| `Infrastructure/DependencyInjection/AddressBookModule.cs` | ~35 | Module DI | COHESIVE |
| `Infrastructure/Persistence/AddressBookDbContext.cs` | ~30 | Module DbContext | COHESIVE |
| `Infrastructure/Persistence/Configurations/CustomerAddressConfiguration.cs` | ~40 | EF entity configuration | COHESIVE |
| `Infrastructure/Outbox/AddressBookOutboxRegistration.cs` | ~28 | Outbox registration | COHESIVE |
| `Infrastructure/Adapters/AddressBookDevelopmentSeed.cs` | ~60 | Development demo data | COHESIVE |
| `Application/Addresses/Commands/*.cs` (4) | ~20 each | Request + handler | COHESIVE |
| `Application/Addresses/Queries/*.cs` (2) | ~30 each | Request + handler | COHESIVE |
| `Application/Addresses/Validators/*.cs` (5) | ~25 each | Transport validator | COHESIVE |
| `Application/Models/CustomerAddressWrite.cs` | ~15 | Write input record | COHESIVE |
| `Application/Ports/IAddressBookDirectory.cs` | ~25 | Module port | COHESIVE |
| `Contracts/*.cs` (4) | ~15–45 | DTO / codes / 2 ports | COHESIVE |
| `Domain/Aggregates/CustomerAddress.cs` | (above) | | |
| `Endpoints/AddressBookEndpointModule.cs` | ~40 | Composition entry | COHESIVE |

Largest hand-written file ≈ **230 LOC** — far below the 800-LOC `ARCH-SIZE-001` threshold. The module has
**no entry** in `Baselines/tmar-source-size-baseline.json`, and it does not appear in the
`TmarSourceSizeAndInfraAppTests` violation inventory (verified in W3).

## Anti-pattern audit

| Anti-pattern | Present? | Note |
| --- | --- | --- |
| new oversized file beyond the size baseline | NO | largest ≈230 LOC |
| new god-file / multi-responsibility file | NO | each file has one reason to change |
| artificial parallel decomposition to game the size guard | NO | W2 **merged** directories; no file was split |
| generic Application `*Contracts.cs` bundle | NO | no such file exists |
| duplicate Application command/query shape beside the authoritative MediatR request | NO | see `cqrs-request-matrix.md` |
| unjustified single-file-per-request directory tree | NO | removed in W2, guarded |
| endpoint transport + request/response models + infrastructure + business logic collapsed into one file | NO | separate files per concern |
| cosmetic splitting producing meaningless tiny files | NO | the smallest files (~15–25 LOC) are single-responsibility records/ports |
| Host platform file migrated into this business module | NO | no Host file touched by AMSC |

## Cohesion rationale for the two "wide" files

1. `AddressBookDirectory.cs` — one EF adapter for one aggregate. Its methods (create, update, delete,
   set-default, list, get, `RequireOwnAsync`, `EnsureActor`, `ComposeRecipient`) all serve the same
   persistence responsibility for the same entity. Splitting it further would create partial adapters over
   one `DbSet` with no cohesion benefit. Classified `OVERSIZED_ONLY`-equivalent, and it is not even
   oversized.
2. `CustomerAddress.cs` — one aggregate with its invariants (`ApplyFields`, `ApplyRecipientNames`,
   `RequireBounded`, `OptionalBounded`). The helper methods are private and used only by the aggregate's
   own invariant enforcement.

No `OVER_SPLIT`, no `MULTI_RESPONSIBILITY_COHESION_VIOLATION`.
