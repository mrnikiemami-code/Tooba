# TB-TMAR-CART-AMSC-001-W0 — Folder Granularity

## Measured state

```text
Folder-Granularity-State = TECHNICAL_AXIS_FIRST
```

Two **top-level technical axes** exist directly under `Tooba.Cart.Application/`:

```text
Application/Commands/
Application/Queries/
```

and each carries per-use-case children. Counting **production source files** (the Structure skill's
counting rule — not declared types):

| Leaf folder | Production `.cs` files | Names | Verdict |
| --- | --- | --- | --- |
| `Application/Commands/AddCartLine/` | 2 | `AddCartLineCommand.cs`, `AddCartLineCommandValidator.cs` | 2 files, but named after **one** use case |
| `Application/Commands/ChangeCartLineQuantity/` | 2 | `…Command.cs`, `…CommandValidator.cs` | 2 files, named after one use case |
| `Application/Commands/CreateGuestCart/` | **1** | `CreateGuestCartCommand.cs` | **OVER_FOLDERED** |
| `Application/Commands/MergeCartAfterLogin/` | **1** | `MergeCartAfterLoginCommand.cs` | **OVER_FOLDERED** |
| `Application/Commands/RemoveCartLine/` | 2 | `…Command.cs`, `…CommandValidator.cs` | 2 files, named after one use case |
| `Application/Queries/GetCart/` | 2 | `GetCartQuery.cs`, `GetCartQueryValidator.cs` | 2 files, named after one use case |
| `Application/Queries/GetCurrentCart/` | **1** | `GetCurrentAuthenticatedCartQuery.cs` | **OVER_FOLDERED** |

**5 unjustified single-file use-case leaves** (`CreateGuestCart`, `MergeCartAfterLogin`,
`GetCurrentCart`) — plus 4 more leaves that exist only to pair one request with its validator under a
use-case-named folder, which the Structure skill §8 explicitly lists as **non-justifying**
("multiple types in one file", "request and handler co-located", "use-case-named folder",
"historical style").

Total use-case-named leaves: **7**. All 7 are unjustified.

## Per-use-case exception test (Structure skill §9) — rejected

A deeper leaf is permitted only when it contains **multiple cohesive production files with distinct
responsibilities** (request / handler / policy / mapper / validator). Every Cart leaf contains either
one file, or exactly the request + its validator — which is the *same* use case's request/validation
pair, not two distinct responsibilities. Applying the exception here would require splitting
`Command.cs` into `Command.cs` + `Handler.cs` purely to justify the folder, which §8 lists as
`OVER_SPLIT` / cosmetic splitting (hard rule 7).

## Technical-axis-first detection (Structure skill §10)

Flagged because:

- `Application/Commands/` and `Application/Queries/` are the **primary** axis at the Application
  root;
- they contain per-use-case children;
- the secondary axes (`Validators`) live in a **third** root-level folder
  (`Application/Validators/`), so a single use case's files are spread across three top-level trees:
  `Commands/AddCartLine/AddCartLineCommand.cs`,
  `Commands/AddCartLine/AddCartLineCommandValidator.cs`,
  `Validation/CartFluentRules.cs`.

The last item is the clearest symptom: **one use case's responsibility is physically scattered**,
which is exactly what capability-first grouping prevents.

## Target shape (W2)

```text
Application/
  Cart/                       <-- plural capability folder
    Commands/
      AddCartLineCommand.cs
      ChangeCartLineQuantityCommand.cs
      CreateGuestCartCommand.cs
      MergeCartAfterLoginCommand.cs
      RemoveCartLineCommand.cs
    Queries/
      GetCartQuery.cs
      GetCurrentAuthenticatedCartQuery.cs
    Validators/
      AddCartLineCommandValidator.cs
      ChangeCartLineQuantityCommandValidator.cs
      GetCartQueryValidator.cs
      RemoveCartLineCommandValidator.cs
  Composition/
  Conversion/            (or folded into Cart/ — W2 decides with evidence)
  Errors/
  Lifetime/
  Ports/
  Presentation/
  Validation/            (shared cross-cutting rules)
```

Resulting leaf counts: `Cart/Commands/` = 5 files, `Cart/Queries/` = 2 files,
`Cart/Validators/` = 4 files — all **multi-file** axes, so no single-file leaf folder remains and the
primary axis is the capability.

## Naming decision — `Cart/` vs `Carts/` vs `ShoppingCart/`

| Candidate | Assessment |
| --- | --- |
| `Cart/` | matches the module name, the schema (`cart`), the DbSet (`Carts`), the code prefix (`cart.*`) and the route (`/cart`) — **chosen** |
| `Carts/` | plural-of-DbSet; sibling modules use the business plural (`Addresses`, `Offers`, `Roles`) but Cart's own vocabulary is singular (`ShoppingCart` aggregate, `cart.*` codes) |
| `ShoppingCart/` | duplicates the aggregate type name; `Cart` is the capability, `ShoppingCart` is the aggregate |

`Cart/` is chosen because the module's own stable vocabulary is singular-`cart`; the plural-capability
convention in siblings exists to distinguish multiple capabilities inside one module, which does not
apply here.

## Durable enforcement to add (W2)

1. `Application/Commands/` and `Application/Queries/` must **not** exist at the Application root.
2. `Application/Cart/` must exist and contain only `Commands`, `Queries`, `Validators`.
3. A source-file-counting single-file-leaf detector scoped to the module, skipping
   `bin`/`obj`/`Migrations` and approved structural folder names (`Commands`, `Queries`,
   `Validators`, `Models`, `Ports`, `Errors`, `Resources`, `Adapters`, `Persistence`,
   `DependencyInjection`, `Events`, `Security`, `Messaging`, `Lifetime`, `Presentation`,
   `Composition`, `Conversion`, `Validation`).
4. `AllowedApplicationFolders` updated to the capability-first set.

## Relation to the existing certificate

`tmar-module-structure-manifests.json` currently records Cart with `structureCertified: true` and
`forbiddenTopLevelFolders: []` for Application. The `forbiddenTopLevelFolders` field is empty, so the
manifest does **not** currently assert the capability-first shape — it only asserts the root
allowlist (`GlobalUsings.Domain.cs`, `GlobalUsings.Layout.cs`), which Cart satisfies. This is a
**manifest completeness gap**, not a false claim; W2/W3 must populate
`forbiddenTopLevelFolders` for `Tooba.Cart.Application` with `Commands`, `Queries`, `Models`, `Ports`
to lock the capability-first shape.
