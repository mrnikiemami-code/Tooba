# TB-TMAR-ADDRESSBOOK-AMSC-001-W2 — folder-granularity

## Defect (W0 F3 / F4)

`Tooba.AddressBook.Application` was organized **primarily by technical axis** with a one-file
use-case leaf folder under each axis:

```text
Commands/CreateCustomerAddress/CreateCustomerAddressCommand.cs
Commands/DeleteCustomerAddress/DeleteCustomerAddressCommand.cs
Commands/SetDefaultCustomerAddress/SetDefaultCustomerAddressCommand.cs
Commands/UpdateCustomerAddress/UpdateCustomerAddressCommand.cs
Queries/GetCustomerAddress/GetCustomerAddressQuery.cs
Queries/ListCustomerAddresses/ListCustomerAddressesQuery.cs
Validators/CreateCustomerAddress/CreateCustomerAddressCommandValidator.cs
Validators/DeleteCustomerAddress/DeleteCustomerAddressCommandValidator.cs
Validators/GetCustomerAddress/GetCustomerAddressQueryValidator.cs
Validators/SetDefaultCustomerAddress/SetDefaultCustomerAddressCommandValidator.cs
Validators/UpdateCustomerAddress/UpdateCustomerAddressCommandValidator.cs
```

**11 unjustified single-file leaf folders.**

## Counting rule applied

The Structure skill counts **production source files**, not declared types. Each of the 11 leaves holds
exactly **one** `.cs` file. The fact that a command file declares *request + handler* (or a query file
declares *request + handler*) does **not** justify the folder — the skill lists "multiple types in one
file", "request and handler co-located", "use-case-named folder" and "historical style" as
**non-justifying** reasons.

Measured per leaf (source files in that directory, `*.csproj`/`*.resx` excluded):

| Leaf folder | Source files | Verdict |
| --- | --- | --- |
| `Commands/CreateCustomerAddress/` | 1 | OVER_FOLDERED |
| `Commands/DeleteCustomerAddress/` | 1 | OVER_FOLDERED |
| `Commands/SetDefaultCustomerAddress/` | 1 | OVER_FOLDERED |
| `Commands/UpdateCustomerAddress/` | 1 | OVER_FOLDERED |
| `Queries/GetCustomerAddress/` | 1 | OVER_FOLDERED |
| `Queries/ListCustomerAddresses/` | 1 | OVER_FOLDERED |
| `Validators/CreateCustomerAddress/` | 1 | OVER_FOLDERED |
| `Validators/DeleteCustomerAddress/` | 1 | OVER_FOLDERED |
| `Validators/GetCustomerAddress/` | 1 | OVER_FOLDERED |
| `Validators/SetDefaultCustomerAddress/` | 1 | OVER_FOLDERED |
| `Validators/UpdateCustomerAddress/` | 1 | OVER_FOLDERED |

## Per-use-case exception test (section 9) — rejected

A deeper leaf is permitted only when it contains **multiple cohesive production files** with distinct
responsibilities (request / handler / policy / mapper / validator). Every AddressBook leaf contains a
single file; no exception applies. The alternative (splitting request and handler into separate files to
"justify" the folder) would be a **cosmetic split**, explicitly forbidden by the skill's hard rule 7.

## Capability discovery (section 5) — `Addresses`

AddressBook has exactly **one** business capability: the customer delivery address book. The name is not
mechanically derived from a command name; it matches the module's own vocabulary:

- Domain aggregate: `CustomerAddress` (`Domain/Aggregates/CustomerAddress.cs`)
- Persistence schema: `address_book`, DbSet `Addresses`
- Contracts read snapshot: `CustomerAddressRecord`
- Routes: `/v1/customer/addresses…`

Sibling precedent for the same single-capability plural shape:

- `UserPreference` → `LocalePreferences/{Commands,Queries,Validators}/`
- `UiPreferences/{Commands,Queries,Validators}/`
- `Wishlist` → `Customer/{Commands,Queries,Validators}/`
- `BulkInquiry` → `Storefront/{Commands,Validators}/`
- `Party` → `Seller/{Commands,Queries,Validators}/`, `Admin/Sellers/{Queries,Validators}/`

## Repair applied

```text
Application/
  Addresses/
    Commands/   <- 4 files (flattened)
    Queries/    <- 2 files (flattened)
    Validators/ <- 5 files (flattened)
  Composition/  (unchanged)
  Models/       (unchanged)
  Ports/        (unchanged)
  Validators/AddressBookFluentRules.cs  (shared, unchanged)
```

- 11 leaf directories deleted from disk.
- 11 files moved with `git mv` (history preserved).
- Primary axis is now **capability-first**; `Commands`/`Queries`/`Validators` are secondary axes
  *under* the capability.
- `Application/Commands/`, `Application/Queries/` no longer exist as top-level folders.
  `Application/Validators/` remains **only** as the shared cross-cutting rules folder (one structural
  file, `AddressBookFluentRules.cs`) — it is not a request tree.

## Durable enforcement added

`AddressBookPhysicalStructureGuardTests`:

1. `AddressBook_application_is_capability_first_with_no_technical_axis_root`
   — fails if `Application/Commands/` or `Application/Queries/` exists at the Application root; requires
   `Application/Addresses/`; rejects any capability sub-tree deeper than
   `Addresses/<approved axis>/…`.
2. `AddressBook_has_no_single_file_use_case_leaf_folders`
   — walks the module, skips `bin/`/`obj/`/`Migrations/`, skips approved **structural** folder names
   (`Commands`, `Queries`, `Validators`, `Models`, `Ports`, `Errors`, `Resources`, `Adapters`, …), skips
   project roots (governed by the root-allowlist guard), and fails on any remaining directory holding
   exactly one production source file.
3. `AllowedApplicationFolders` is now capability-first
   (`Addresses`, `Composition`, `Models`, `Ports`, `Validators`, `Dtos`, `ReadModels`, `Mappings`,
   `Policies`); bare `Commands`/`Queries` at the Application root are no longer approved.

The regex-based variant that first lived in `AddressBookValidatorCoverageGuardTests` was **consolidated
into the physical-structure guard** so there is exactly one owner of physical-layout enforcement; the
validator-coverage guard was returned to its own concern (endpoint-reachable request/validator coverage).
