# TB-TMAR-CART-AMSC-001-W2 — Structure (AMSC Wave 2)

- Task: `TB-TMAR-CART-AMSC-001-W2`
- Mode: `IMPLEMENTATION`
- Skill: `.cursor/skills/tooba-architecture-structure/SKILL.md`
- Target: `src/backend/Modules/Cart/Tooba.Cart.*`
- Branch: `main`
- Predecessor: `TB-TMAR-CART-AMSC-001-W1` (commit `a5ddc052`)
- W0/W1 handoff: `Folder-Granularity-State = TECHNICAL_AXIS_FIRST`, `structureHandoffState = REQUIRED`

## 1. Closed findings

| Finding | State before | State after |
| --- | --- | --- |
| F7 `TECHNICAL_AXIS_FIRST` | `Application/{Commands,Queries,Validators}` at the Application root, with 7 use-case-named leaf folders | `Application/Carts/{Commands,Queries,Validators}` — capability-first, no leaf folder |
| F9 `Models` tombstone | `Application/Models/CartPage.cs` comment-only, declares no type | **deleted** |
| D1 manifest coverage | 3 of 5 production projects listed | **5 of 5** (`Contracts` + `Domain` added) |
| D2 manifest lock | `forbiddenTopLevelFolders: []` for every Cart project | `Tooba.Cart.Application` locks `["Commands","Queries","Models"]` |
| G8 namespace mirroring | `AssertNamespacesAlign` compared only the **first** folder segment, so it silently accepted the deep technical-axis shape | now compares the **full** folder path, with a documented flat-namespace allowance for the technical axes |

## 2. Capability discovery (skill §5)

`Cart` has exactly **one** business capability: the storefront shopping cart. The capability name is
not mechanically derived from a command name — it is the module's own vocabulary:

| Signal | Value |
| --- | --- |
| Domain aggregate | `ShoppingCart` |
| Persistence schema | `cart` |
| DbSet | `Carts` |
| Route group | `/v1/storefront/cart…` |
| Integration events | `cart.created.v1`, `cart.line_added.v1`, … |
| Stable code prefix | `cart.*` |
| Contracts gateway | `ICartQueryGateway`, `ICartConversionPort` |

### Naming decision — `Carts/` (plural)

W0 proposed `Cart/` (singular). W2 changed this to **`Carts/`** on evidence:

| Evidence | Implication |
| --- | --- |
| `CartDbContext` DbSet is `Carts` | the module's own plural is `Carts` |
| The stable code prefix is `cart.` (already carrying the module identity) | a `Cart` capability folder would read `Tooba.Cart.Application.Cart.*` — the module name duplicated inside itself |
| Sibling convention: `AddressBook/Addresses`, `Offer/Offers`, `Party/Seller` + `Party/Admin/Sellers`, `Content/Articles` | the capability folder is the **business plural** |
| `Tooba.Cart.Application.Carts.Queries` vs `Tooba.Cart.Application.Queries` | the plural also disambiguates cleanly against the technical axis names |

`Carts/` is therefore both the module's own vocabulary **and** the sibling convention.

## 3. Physical move (11 files, `git mv` — history preserved)

```text
Application/
  Carts/                                    <-- capability
    Commands/                               <-- 5 files (multi-file axis)
      AddCartLineCommand.cs
      ChangeCartLineQuantityCommand.cs
      CreateGuestCartCommand.cs
      MergeCartAfterLoginCommand.cs
      RemoveCartLineCommand.cs
    Queries/                                <-- 2 files (multi-file axis)
      GetCartQuery.cs
      GetCurrentAuthenticatedCartQuery.cs
    Validators/                             <-- 4 files (multi-file axis)
      AddCartLineCommandValidator.cs
      ChangeCartLineQuantityCommandValidator.cs
      GetCartQueryValidator.cs
      RemoveCartLineCommandValidator.cs
  Composition/          (shared seam, unchanged)
  Conversion/           (shared seam, unchanged)
  Lifetime/             (shared, unchanged)
  Ports/                (shared, unchanged)
  Presentation/         (shared, unchanged)
  Validation/           (shared cross-cutting rules, unchanged)
```

Deleted: `Application/Commands/`, `Application/Queries/`, `Application/Models/` (11 leaf directories +
1 tombstone file). **No single-file leaf folder remains.**

| Axis | Files before | Files after | Leaf folders before | Leaf folders after |
| --- | --- | --- | --- | --- |
| `Commands` | 5 (across 5 leaves) | 5 (flat) | 5 | 0 |
| `Queries` | 2 (across 2 leaves) | 2 (flat) | 2 | 0 |
| `Validators` | 4 (across 4 leaves) | 4 (flat) | 4 | 0 |

## 4. Namespace decisions

| Before | After |
| --- | --- |
| `Tooba.Cart.Application.Commands.AddCartLine` | `Tooba.Cart.Application.Carts.Commands` |
| `Tooba.Cart.Application.Commands.ChangeCartLineQuantity` | `Tooba.Cart.Application.Carts.Commands` |
| `Tooba.Cart.Application.Commands.CreateGuestCart` | `Tooba.Cart.Application.Carts.Commands` |
| `Tooba.Cart.Application.Commands.MergeCartAfterLogin` | `Tooba.Cart.Application.Carts.Commands` |
| `Tooba.Cart.Application.Commands.RemoveCartLine` | `Tooba.Cart.Application.Carts.Commands` |
| `Tooba.Cart.Application.Queries.GetCart` | `Tooba.Cart.Application.Carts.Queries` |
| `Tooba.Cart.Application.Queries.GetCurrentCart` | `Tooba.Cart.Application.Carts.Queries` |
| `…Commands.AddCartLine` (validator) | `Tooba.Cart.Application.Carts.Validators` |
| `…Commands.ChangeCartLineQuantity` (validator) | `Tooba.Cart.Application.Carts.Validators` |
| `…Commands.RemoveCartLine` (validator) | `Tooba.Cart.Application.Carts.Validators` |
| `…Queries.GetCart` (validator) | `Tooba.Cart.Application.Carts.Validators` |

This matches the `AddressBook` precedent exactly (`Tooba.AddressBook.Application.Addresses.Validators`
for a validator physically under `Addresses/Validators/`), i.e. the **axis folder** owns the namespace
and the per-use-case namespace level is removed.

Because the four request+validator pairs no longer share a namespace, the validators gained an explicit
`using Tooba.Cart.Application.Carts.Commands;` (3) / `…Carts.Queries;` (1).

Consumers updated:

| File | Change |
| --- | --- |
| `Tooba.Cart.Endpoints/Storefront/CartStorefrontEndpoints.cs` | 7 usings collapsed to 2 (`Carts.Commands`, `Carts.Queries`) |
| `Tooba.Cart.Tests/Behavior/CartMulticurrencyTests.cs` | + `Carts.Commands`, + `Carts.Validators` |
| `Tooba.Cart.Tests/Validation/CartEndpointValidatorCoverageGuardTests.cs` | + `Carts.Commands`, `Carts.Queries`, `Carts.Validators` |
| `Host/Tooba.Host/Program.cs` | MediatR assembly scan probe retargeted |

## 5. Shared-folder decisions (explicit, not accidental)

The following stay at the Application root because they are **shared cross-cutting concerns of the one
capability**, not second capabilities. This is the same allowance the `AddressBook` guard encodes
(`Addresses` + `Composition` + `Models` + `Ports` + `Validators`).

| Folder | Why it stays at the root |
| --- | --- |
| `Ports/` | implementation seams (`ICartDirectory`, `ICartCommerceContextResolver`) consumed by Infrastructure **and** by `Catalog.Application` through the Contracts port |
| `Lifetime/` | expiry reconciler + persistence-policy ports consumed by Infrastructure workers |
| `Conversion/` | the Order-facing conversion seam |
| `Presentation/` | one storefront projection for the whole capability |
| `Composition/` | the W1 fault→`Result` seam (mirrors 16 sibling modules) |
| `Validation/` | `CartFluentRules` / `CartValidationCodes` shared by all 4 validators |

Only `Commands`, `Queries` and `Models` are locked as forbidden at the Application root — the exact
shape W2 removed. `Ports` is deliberately **not** forbidden because the evidence above shows it is a
shared seam, and the `AddressBook` precedent keeps it at the root.

## 6. Durable guards

### `CartArchitectureGuardTests` (module guard)

| Change | Justification |
| --- | --- |
| `AllowedApplicationFolders` → `["Carts","Ports","Lifetime","Conversion","Composition","Presentation","Validation"]` | capability-first set; `Commands`/`Queries`/`Models` removed from the **root** allowlist |
| new `Cart_application_is_capability_first_with_no_technical_axis_root` | fails if `Application/Commands` or `Application/Queries` exists at the root; requires `Application/Carts`; rejects any folder under `Carts/` other than `Commands`/`Queries`/`Validators`; rejects any deeper leaf; requires all three axes to be multi-file |
| new `Cart_has_no_single_file_use_case_leaf_folders` | walks the Application tree, skips `bin`/`obj`/`artifacts`/`Migrations` and approved **structural** folder names, fails on any remaining directory holding exactly one production `.cs` |
| new `Cart_directory_stays_under_the_arch_size_ceiling` | freezes W1's `ARCH-SIZE-001` repair so `CartDirectory.cs` cannot silently regrow past 800 LOC |
| `AssertNamespacesAlign` strengthened | now compares the **full** folder path to the namespace instead of only the first segment (the old version could not detect the deep technical-axis shape), with a documented flat-namespace allowance for `Commands`/`Queries`/`Validators` |

The single-file-leaf detector is scoped to `Tooba.Cart.Application` on purpose: `Contracts/Checkout`
(11 root-namespace types) and the test project's own folders are governed by their own allowlist
arrays and are **not** request trees. Scoping is recorded in the test's comment, not silently applied.

### `HostCartResidualGuardTests` (Host guard)

| Change | Justification |
| --- | --- |
| `CreateGuestCart_uses_commerce_context_without_hardcoded_values` path | `Commands/CreateGuestCart/CreateGuestCartCommand.cs` → `Carts/Commands/CreateGuestCartCommand.cs`. The guard's **assertions are unchanged** (commerce-context resolution, no hard-coded `"IR"`/`"IRR"`/`SalesChannel`). |

No guard was deleted. No assertion was removed or weakened. `guardsWeakened = NONE`.

## 7. Manifest / SoT

`docs/architecture/tmar-module-structure-manifests.json` → `Cart` entry:

- `Tooba.Cart.Contracts` **added** — `rootAllowlist: []`, forbidden root files
  (`CartContracts.cs`, `CartErrorCodes.cs`, `CartPresentationContracts.cs`).
- `Tooba.Cart.Domain` **added** — `rootAllowlist: []`, forbidden root files (`ShoppingCart.cs`,
  `CartLine.cs`).
- `Tooba.Cart.Application.forbiddenTopLevelFolders` → `["Commands","Queries","Models"]`.
- `certificationNote` records the W1 fault mechanism and the W2 capability-first shape.
- `structureCertified` stays `true`; `lockVersion` stays `ARCH-COMPLETE-002`. **No module was added to
  or removed from `modules` / `uncertifiedHttpOwningModules` / `preCertModules`.**

Solution Explorer (`Tooba.slnx`) was **already** canonical (`/Modules/Cart/` containing all 6 Cart
projects in layer order) and is unchanged. No solution-file edit was needed.

## 8. Verification

```text
dotnet build src/backend/Tooba.slnx      → 0 errors
dotnet test Tooba.Cart.Tests             → 27 passed / 0 failed / 0 skipped
                                            (W1: 24; +3 new structure/size guards)
dotnet test Tooba.Host.Tests             → 91 failed / 1761 passed / 130 skipped
                                            (set-identical to the pre-existing baseline — zero regressions)
```

## 9. Invariants preserved

| Invariant | Status |
| --- | --- |
| Zero foreign Application/Infrastructure/Domain coupling | preserved |
| `ICartDirectory` / `ICartQueryGateway` / `ICartConversionPort` signatures | unchanged |
| CQRS request/handler types (not renamed, only moved) | preserved |
| Validator coverage (4 required + 3 no-validator) | preserved |
| Schema `cart`, migrations, snapshot | untouched |
| DI registration, Outbox, DbContext | untouched |
| Host production files | **zero modified** |
| `HOST_FINAL_CLOSURE_REGRESSION` | `NONE` |
| `microserviceExtractable` | `true` |

## 10. Handoff

```text
W2 exit state: FolderGranularityState = CAPABILITY_FIRST_SHALLOW
               PathNamespaceState      = EXACT_FULL_PATH_MIRRORED
               PhysicalCopyState       = NO_TOMBSTONE
               ManifestStructureState  = ALL_5_PROJECTS_LOCKED
               SolutionExplorerState   = CANONICAL (unchanged)
               structureHandoffState   = READY_FOR_CERTIFY
```

`automaticNextTask = TB-TMAR-CART-AMSC-001-W3` (skill `tooba-architecture-certify`).
