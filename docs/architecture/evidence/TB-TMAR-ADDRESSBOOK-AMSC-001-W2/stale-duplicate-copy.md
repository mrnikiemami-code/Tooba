# TB-TMAR-ADDRESSBOOK-AMSC-001-W2 — stale-duplicate-copy

`Physical-Copy-State = CLEAN`.

## Leftover-path check after the W2 move

The 11 legacy use-case leaf directories were **deleted from disk** (not merely emptied), so no file exists
at both an old and a new path:

```text
Application/Commands/CreateCustomerAddress/            DELETED
Application/Commands/DeleteCustomerAddress/            DELETED
Application/Commands/SetDefaultCustomerAddress/        DELETED
Application/Commands/UpdateCustomerAddress/            DELETED
Application/Queries/GetCustomerAddress/                DELETED
Application/Queries/ListCustomerAddresses/             DELETED
Application/Validators/CreateCustomerAddress/          DELETED
Application/Validators/DeleteCustomerAddress/          DELETED
Application/Validators/GetCustomerAddress/             DELETED
Application/Validators/SetDefaultCustomerAddress/      DELETED
Application/Validators/UpdateCustomerAddress/          DELETED
Application/Commands/                                   DELETED (now empty, removed)
Application/Queries/                                    DELETED (now empty, removed)
```

`git status --porcelain` reports the moves as renames (`R`) with no residual `D`+`??` pair, i.e. the old
paths are gone from both the working tree and the index.

## Duplicate-responsibility check

`AddressBookPhysicalStructureGuardTests.AddressBook_has_no_stale_or_duplicate_physical_type_copies` walks the
whole module (excluding `bin/`, `obj/`, `Migrations/`, `*ModelSnapshot.cs`), keys by file name and fails on
any file name appearing twice. **PASS** — each of the 11 flattened files has exactly one live home.

No type is declared in two live paths; no two files own the same responsibility. The two
near-identical write records (`Application/Models/CustomerAddressWrite.cs` vs the endpoint's
`CustomerAddressWriteRequest`) are **not** duplicate copies — they are the sanctioned boundary split
(W0 F7): the HTTP body strips owner identity before the Application input is constructed. That split is
asserted by `AddressBookFoundationTests` and is preserved.

## Solution-entry integrity

No `.slnx` entry points at a deleted path: the Solution Folder groups **projects**, and no project was moved
or renamed. All 5 entries still resolve.

## EF artifact exemptions

`Persistence/Migrations/*` and `AddressBookDbContextModelSnapshot.cs` are EF-generated and intentionally
excluded from type-uniqueness and single-file-leaf checks (repository lock). They were not touched by W2;
no migration was regenerated and no schema changed.
