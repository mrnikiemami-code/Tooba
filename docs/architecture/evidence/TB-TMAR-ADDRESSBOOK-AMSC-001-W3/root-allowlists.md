# TB-TMAR-ADDRESSBOOK-AMSC-001-W3 — root-allowlists

`Root-Allowlist-State = ENFORCED`.

Enforcement owner: `TmarCompleteReferenceStructureGateTests.Certified_modules_satisfy_root_allowlists_and_namespace_alignment`
and `AddressBookModuleAmsc001W3CertGuardTests.AddressBook_manifest_entry_is_single_and_disk_reconciled`.

## Measured root files vs manifest

| Project | Root `*.cs` on disk | `rootAllowlist` | Match |
| --- | --- | --- | --- |
| `Tooba.AddressBook.Contracts` | *(none)* | `[]` | EXACT |
| `Tooba.AddressBook.Domain` | *(none)* | `[]` | EXACT |
| `Tooba.AddressBook.Application` | *(none)* | `[]` | EXACT |
| `Tooba.AddressBook.Endpoints` | `AddressBookEndpointModule.cs` | `["AddressBookEndpointModule.cs"]` | EXACT |
| `Tooba.AddressBook.Infrastructure` | *(none)* | `[]` | EXACT |

## Forbidden root files — verified absent

| Project | `forbiddenRootFiles` | Present? |
| --- | --- | --- |
| Contracts | `CustomerAddressContracts.cs` | No |
| Domain | *(none)* | — |
| Application | `AddressBookContracts.cs` | No |
| Endpoints | `AddressBookCustomerReadEndpoints.cs`, `AddressBookCustomerWriteEndpoints.cs`, `AddressBookCustomerActorResolver.cs` | No |
| Infrastructure | `AddressBookModule.cs`, `AddressBookDirectory.cs` | No |

## Forbidden top-level folders — verified absent

| Project | `forbiddenTopLevelFolders` | Present? |
| --- | --- | --- |
| Infrastructure | `["Migrations"]` | No (migrations live under `Persistence/Migrations/`) |

## Allowlist integrity

| Check | Result |
| --- | --- |
| Any allowlist widened during AMSC? | NO |
| Any `forbiddenRootFiles` entry removed? | NO |
| Any `forbiddenTopLevelFolders` entry removed? | NO |
| Any forbidden-root file resurrected? | NO |
| `Root-Dump-State` | ZERO |

The only AMSC manifest edits to `Tooba.AddressBook.Application` were **prose** corrections to
`rootAllowlistJustification` (W2) so the documented structure matches the repaired structure. The
allowlist value itself stayed `[]` — the W2 move kept the Application root free of `.cs` files.
