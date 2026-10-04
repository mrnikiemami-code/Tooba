# TB-TMAR-CART-AMSC-001-W2 — folder-granularity

## Defect (W0 F7)

`Tooba.Cart.Application` was organized **primarily by technical axis** with a per-use-case leaf folder
under each axis:

```text
Commands/AddCartLine/AddCartLineCommand.cs
Commands/AddCartLine/AddCartLineCommandValidator.cs
Commands/ChangeCartLineQuantity/ChangeCartLineQuantityCommand.cs
Commands/ChangeCartLineQuantity/ChangeCartLineQuantityCommandValidator.cs
Commands/CreateGuestCart/CreateGuestCartCommand.cs
Commands/MergeCartAfterLogin/MergeCartAfterLoginCommand.cs
Commands/RemoveCartLine/RemoveCartLineCommand.cs
Commands/RemoveCartLine/RemoveCartLineCommandValidator.cs
Queries/GetCart/GetCartQuery.cs
Queries/GetCart/GetCartQueryValidator.cs
Queries/GetCurrentCart/GetCurrentAuthenticatedCartQuery.cs
```

**7 unjustified use-case-named leaf folders.**

## Counting rule applied

The Structure skill counts **production source files**, not declared types. Three leaves held exactly
one file; four held the request + its own validator — which the skill lists as **non-justifying**
("multiple types in one file", "request and handler co-located", "use-case-named folder",
"historical style").

| Leaf folder | Source files | Verdict |
| --- | --- | --- |
| `Commands/AddCartLine/` | 2 | OVER_FOLDERED |
| `Commands/ChangeCartLineQuantity/` | 2 | OVER_FOLDERED |
| `Commands/CreateGuestCart/` | 1 | OVER_FOLDERED |
| `Commands/MergeCartAfterLogin/` | 1 | OVER_FOLDERED |
| `Commands/RemoveCartLine/` | 2 | OVER_FOLDERED |
| `Queries/GetCart/` | 2 | OVER_FOLDERED |
| `Queries/GetCurrentCart/` | 1 | OVER_FOLDERED |

## Per-use-case exception test (section 9) — rejected

A deeper leaf is permitted only when it contains **multiple cohesive production files with distinct
responsibilities** (request / handler / policy / mapper / validator). Splitting a command file into
`Command.cs` + `Handler.cs` purely to justify the folder is a **cosmetic split**, forbidden by the
skill's hard rule 7.

## Repair applied

```text
Application/
  Carts/
    Commands/   <- 5 files (flattened, multi-file axis)
    Queries/    <- 2 files (flattened, multi-file axis)
    Validators/ <- 4 files (flattened, multi-file axis)
  Composition/  (unchanged)
  Conversion/   (unchanged)
  Lifetime/     (unchanged)
  Ports/        (unchanged)
  Presentation/ (unchanged)
  Validation/   (shared cross-cutting rules, unchanged)
```

- 11 leaf directories deleted from disk; `Application/Models/` (comment-only tombstone) deleted.
- 11 files moved with `git mv` (history preserved).
- The primary axis is now **capability-first**; `Commands`/`Queries`/`Validators` are secondary axes
  *under* the capability.
- `Application/Commands/`, `Application/Queries/` and `Application/Models/` no longer exist.

## Capability name — `Carts`

See `structure.md` §2. Summary: the DbSet is `Carts`, the sibling convention is the business plural
(`Addresses`, `Offers`, `Articles`), and the plural avoids
`Tooba.Cart.Application.Cart.*` (module name duplicated inside itself).

## Durable enforcement added

`CartArchitectureGuardTests`:

1. `Cart_application_is_capability_first_with_no_technical_axis_root`
   — fails if `Application/Commands` or `Application/Queries` exists at the Application root; requires
   `Application/Carts`; rejects any folder under `Carts/` other than `Commands`/`Queries`/`Validators`;
   rejects any deeper leaf; requires all three axes to hold more than one source file.
2. `Cart_has_no_single_file_use_case_leaf_folders`
   — walks the **Application** tree, skips `bin/`/`obj/`/`artifacts/`/`Migrations/`, skips approved
   structural folder names, and fails on any remaining directory holding exactly one production `.cs`.
3. `AllowedApplicationFolders` is now capability-first; bare `Commands`/`Queries`/`Models` at the
   Application root are no longer approved.
4. `AssertNamespacesAlign` now compares the **full** folder path (the previous implementation compared
   only the first segment and therefore could not detect the deep technical-axis shape).

## Not repaired (recorded)

| Item | Why |
| --- | --- |
| `Application/Ports/` still mixes lifetime / commerce / directory / persistence-hours ports (W0 F11) | Evidence shows they are all shared implementation seams of the single capability; `AddressBook` keeps a root `Ports/` for the same reason. Consolidating them into `Lifetime/` would move a port that `Catalog.Application` resolves through the Contracts port and gains no cohesion. Recorded as accepted shape, not debt. |
| `Application/Ports/ICartPersistenceHoursSource.cs` empty alias (W0 F6) | Load-bearing: `Catalog.Application` resolves the Application alias in two handlers and `HostCartResidualGuardTests` asserts the file's existence at that exact path. Removing it would be a cross-module change outside this task's bounded scope. |
| `Application/Validation/` vs `Validators/` coexistence | Not a defect: `Validation/` holds the shared `CartFluentRules`/`CartValidationCodes`; `Carts/Validators/` holds the 4 request validators. This mirrors `AddressBook` exactly (`Validators/AddressBookFluentRules.cs` + `Addresses/Validators/*`). |
