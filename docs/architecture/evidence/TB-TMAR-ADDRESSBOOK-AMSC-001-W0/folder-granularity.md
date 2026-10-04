# TB-TMAR-ADDRESSBOOK-AMSC-001-W0 — Folder Granularity

`Folder-Granularity-State = TECHNICAL_AXIS_FIRST`

## Evidence

`Tooba.AddressBook.Application` organizes primarily by **technical axis** with a one-file use-case
leaf folder under each axis:

```text
Application/
  Commands/<UseCase>/<UseCase>Command.cs          (4 leaves, 1 file each)
  Queries/<UseCase>/<UseCase>Query.cs             (2 leaves, 1 file each)
  Validators/<UseCase>/<UseCase>…Validator.cs     (5 leaves, 1 file each)
```

## Unjustified single-file leaf folders (11)

Source-file count is what matters, not the number of types declared in the file. Each
command/query file declares both the request record and its handler; per the Structure skill this
does **not** justify the folder.

| # | Folder | Production source files | Verdict |
| --- | --- | --- | --- |
| 1 | `Commands/CreateCustomerAddress/` | 1 | `OVER_FOLDERED` |
| 2 | `Commands/DeleteCustomerAddress/` | 1 | `OVER_FOLDERED` |
| 3 | `Commands/SetDefaultCustomerAddress/` | 1 | `OVER_FOLDERED` |
| 4 | `Commands/UpdateCustomerAddress/` | 1 | `OVER_FOLDERED` |
| 5 | `Queries/GetCustomerAddress/` | 1 | `OVER_FOLDERED` |
| 6 | `Queries/ListCustomerAddresses/` | 1 | `OVER_FOLDERED` |
| 7 | `Validators/CreateCustomerAddress/` | 1 | `OVER_FOLDERED` |
| 8 | `Validators/DeleteCustomerAddress/` | 1 | `OVER_FOLDERED` |
| 9 | `Validators/GetCustomerAddress/` | 1 | `OVER_FOLDERED` |
| 10 | `Validators/SetDefaultCustomerAddress/` | 1 | `OVER_FOLDERED` |
| 11 | `Validators/UpdateCustomerAddress/` | 1 | `OVER_FOLDERED` |

## Non-leaf folders (accepted)

| Folder | Files | Verdict |
| --- | --- | --- |
| `Models/` | 1 (`CustomerAddressWrite.cs`) | shared Application-internal model — accepted |
| `Ports/` | 1 (`IAddressBookDirectory.cs`) | shared Application-internal port — accepted |
| `Validators/` (root) | 1 (`AddressBookFluentRules.cs`) | shared cross-cutting rules — accepted |

A shared folder holding one cohesive cross-capability file is **not** a use-case leaf and is
explicitly excluded by the Structure skill's "legitimate single-file folders outside semantic
request/use-case trees" clause.

## Capability analysis

| Question | Answer |
| --- | --- |
| Does AddressBook have multiple business capabilities? | **No** — exactly one: the customer delivery address book. |
| Do the leaves hold multiple cohesive source files? | **No** — 1 file each. |
| Does use-case complexity materially benefit from isolation? | **No** — each command/query is a ~25 LOC record + one-line handler delegation. |
| Is there an existing module convention for a single-capability module? | **Yes** — `UserPreference` (`LocalePreferences/Commands/UpsertUserPreferenceCommand.cs`, no per-use-case leaf), `Party`, `BulkInquiry`, `Wishlist`. |

## Sibling comparison (evidence that the shallow shape is the repository convention)

| Module | Application tree | Per-use-case leaf folders? |
| --- | --- | --- |
| `UserPreference` | `LocalePreferences/{Commands,Queries,Validators}`, `UiPreferences/{Commands,Queries,Validators}`, `Models/`, `Ports/`, `Composition/` | no |
| `BulkInquiry` | `Storefront/{Commands,Validators}`, `Models/`, `Ports/`, `Composition/` | no |
| `Wishlist` | `Customer/{Commands,Queries,Validators}`, `Models/`, `Ports/`, `Composition/` | no |
| `Party` | `Admin/Sellers/{Queries,Validators}`, `Seller/{Commands,Models,Queries,Validators}`, `Models/`, `Ports/`, `Composition/` | no |
| `Offer` (certified reference) | `Offers/{Commands/<UseCase>,Queries/<UseCase>,…}` | yes — but 9 of 11 leaves hold 2 cohesive files (`Command` + `Validator`), so the exception rule applies |
| `AddressBook` | `Commands/<UseCase>`, `Queries/<UseCase>`, `Validators/<UseCase>` | yes — **11 leaves, all 1 file** |

Note that `Offer`'s leaves are **not** a licence for AddressBook's: Offer's leaves carry the
command *and* its validator together, which is the documented multi-file exception. AddressBook
splits the same two files across two different technical-axis trees, producing **two** single-file
leaves where Offer has one justified leaf.

## Certification drift

`docs/architecture/tmar-module-structure-manifests.json` (AddressBook entry, line 1425) records the
non-canonical shape as accepted:

> `"AddressBook.Application intentionally has no root .cs file: Commands/<UseCase>, Queries/<UseCase>, Models, Ports and the shared Validators capability carry every type, and namespace alignment is exact path-derived equality."`

The existing certificate therefore **hides** the folder-granularity defect. W2 must flatten the tree
and correct this justification honestly; W3 must not certify the current tree.

## Required repair (W2)

```text
Application/
  Addresses/                         (plural capability folder)
    Commands/                        (4 files, flat)
    Queries/                         (2 files, flat)
    Validators/                      (5 files, flat)
    Models/CustomerAddressWrite.cs   (or keep shared Models/ — W2 decides with evidence)
    Ports/IAddressBookDirectory.cs   (or keep shared Ports/ — W2 decides with evidence)
  Composition/AddressBookOperation.cs
  Validators/AddressBookFluentRules.cs   (shared cross-cutting — unchanged)
```

No god-file is created by flattening: the largest resulting file stays ~26 LOC, and each flattened
folder holds a cohesive set of same-kind requests.
