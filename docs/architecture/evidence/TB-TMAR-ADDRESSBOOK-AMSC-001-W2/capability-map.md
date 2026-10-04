# TB-TMAR-ADDRESSBOOK-AMSC-001-W2 — capability-map

`Capability discovery` per Structure skill section 5. Capabilities are business responsibility axes, not
invented folder names, and the names are taken from the module's own vocabulary.

## Discovered capabilities

| Capability | Plural folder | Evidence of the name in the module | Public routes |
| --- | --- | --- | --- |
| Customer delivery address book | `Application/Addresses/` | Domain aggregate `CustomerAddress`; EF schema `address_book`; `DbSet<CustomerAddress> Addresses`; `CustomerAddressRecord`; routes `/v1/customer/addresses…` | 6 |

AddressBook is a **single-capability** module. The skill does not require ceremony folders for a
single-capability module, but it *does* require that (a) the primary axis is the capability and (b) no
unjustified single-file use-case leaf folders exist. Both now hold.

## Capability → technical-axis → files

| Capability | Axis | Files |
| --- | --- | --- |
| `Addresses` | `Commands` | `CreateCustomerAddressCommand.cs`, `DeleteCustomerAddressCommand.cs`, `SetDefaultCustomerAddressCommand.cs`, `UpdateCustomerAddressCommand.cs` |
| `Addresses` | `Queries` | `GetCustomerAddressQuery.cs`, `ListCustomerAddressesQuery.cs` |
| `Addresses` | `Validators` | `CreateCustomerAddressCommandValidator.cs`, `DeleteCustomerAddressCommandValidator.cs`, `GetCustomerAddressQueryValidator.cs`, `SetDefaultCustomerAddressCommandValidator.cs`, `UpdateCustomerAddressCommandValidator.cs` |

## Shared / cross-cutting (deliberately NOT inside the capability)

| Folder | Responsibility | Why shared | Precedent |
| --- | --- | --- | --- |
| `Application/Composition/` | `AddressBookOperation` fault→Result seam used by all 6 handlers | Cross-cutting composition | `Content/Composition/ContentOperation.cs`, `AccessControl/Composition/AccessControlOperation.cs`, `UserPreference`, `Wishlist`, `Party`, `BulkInquiry` |
| `Application/Models/` | `CustomerAddressWrite` Application input | Cross-cutting write input | `Content/Articles/Models`, `AccessControl/Models` |
| `Application/Ports/` | `IAddressBookDirectory` | Cross-cutting module port | `AccessControl/Ports/IAccessControlDirectory.cs`, `Party/Ports/IPartyDirectory.cs` |
| `Application/Validators/` | `AddressBookFluentRules` + `AddressBookValidationCodes` | Shared rules reused by every per-request validator | `Content/Validators/ContentValidationCodes.cs`, `AccessControl/Validation/*` |

## Explicit non-capabilities

The following were **not** promoted to capability folders (they are not business axes):

- `Commands`, `Queries`, `Validators` — technical axes. They are secondary and live *under* `Addresses`.
- `Endpoints` — HTTP transport concern; its own audience folder is `Endpoints/Customer/` (unchanged).
- `Persistence`, `Adapters`, `Outbox`, `DependencyInjection` — Infrastructure concerns (unchanged).

## Reference modules used

- `UserPreference` (single-capability, shallow `LocalePreferences/{Commands,Queries,Validators}/`) —
  **REFERENCE_NOT_CLONE**.
- `Content` (multi-capability capability-first: `Articles/`, `Authors/`, `Categories/`, `Tags/`, `Media/`,
  `Comments/`) — reference for the capability-first rule at scale.
- `AccessControl` (the immediately preceding AMSC run, `Roles/`, `Assignments/`, `Permissions/`,
  `Ceiling/`, `Access/`) — reference for consistency with the previous AMSC delivery.
