# TB-TMAR-ADDRESSBOOK-AMSC-001-W2 — root-allowlist

`Root-Allowlist-State = ENFORCED`.

Enforcement owner: `TmarCompleteReferenceStructureGateTests.Certified_modules_satisfy_root_allowlists_and_namespace_alignment`
(reads `docs/architecture/tmar-module-structure-manifests.json` and compares each project's
`rootAllowlist` to the actual top-level `*.cs` files on disk).

## Measured root files (W2)

| Project | Root `*.cs` on disk | `rootAllowlist` | `forbiddenRootFiles` | Verdict |
| --- | --- | --- | --- | --- |
| `Tooba.AddressBook.Contracts` | *(none)* | `[]` | `CustomerAddressContracts.cs` | ENFORCED |
| `Tooba.AddressBook.Domain` | *(none)* | `[]` | `[]` | ENFORCED |
| `Tooba.AddressBook.Application` | *(none)* | `[]` | `AddressBookContracts.cs` | ENFORCED |
| `Tooba.AddressBook.Endpoints` | `AddressBookEndpointModule.cs` | `["AddressBookEndpointModule.cs"]` | `AddressBookCustomerReadEndpoints.cs`, `AddressBookCustomerWriteEndpoints.cs`, `AddressBookCustomerActorResolver.cs` | ENFORCED |
| `Tooba.AddressBook.Infrastructure` | *(none)* | `[]` | `AddressBookModule.cs`, `AddressBookDirectory.cs` | ENFORCED |

W2 moved **no** file to or from a project root, so no allowlist value changed. The
`forbiddenTopLevelFolders` entries were re-checked:

| Project | `forbiddenTopLevelFolders` | Present on disk? |
| --- | --- | --- |
| `Tooba.AddressBook.Infrastructure` | `["Migrations"]` | No (migrations live under `Persistence/Migrations/`) |

## Allowlists NOT widened

No `rootAllowlist` was widened and no `forbiddenRootFiles` / `forbiddenTopLevelFolders` entry was removed
to make the tree pass. The only manifest edits in W2 are **prose corrections** to
`Tooba.AddressBook.Application.rootAllowlistJustification` and the module `certificationNote` so that the
documented structure matches the repaired structure (see `manifest-structure.md`). Widening an allowlist to
hide debt is forbidden by skill section 19 and did not happen.

## Root-dump check

`ROOT_DUMP` is absent: no capability/implementation file sits at any project root except the one approved
composition entry `AddressBookEndpointModule.cs`.
