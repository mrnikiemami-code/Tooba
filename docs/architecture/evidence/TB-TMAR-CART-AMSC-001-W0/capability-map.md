# TB-TMAR-CART-AMSC-001-W0 — Capability Map

## Capability discovery (Structure skill §5)

`Cart` has **one** business capability: the storefront shopping cart — guest/authenticated cart
lifecycle, its lines, its pricing quotes, its expiry and its conversion seam.

Evidence the name `Cart` (not mechanically derived from a command name) is the module's own
vocabulary:

| Signal | Value |
| --- | --- |
| Domain aggregate | `ShoppingCart` (`Domain/Aggregates/ShoppingCart.cs`) |
| Domain entity | `CartLine` (`Domain/Entities/CartLine.cs`) |
| Persistence schema | `cart` (`CartDbContext.Schema`) |
| DbSet | `Carts` (+ owned `Lines`) |
| Root route group | `/v1/storefront/cart…` |
| Contracts ports | `ICartQueryGateway`, `ICartConversionPort`, `ICartPresentationGateway` |
| Integration events | `cart.created.v1`, `cart.line_added.v1`, … |
| Stable code prefix | `cart.*` |

The single-capability shape is not a shortcut: `Cart` is one aggregate boundary with one lifecycle
state machine. Splitting it into pseudo-capabilities (`Lines`, `Expiry`, `Merge`) would invent
capabilities that do not exist and would fragment one invariant across folders.

## Capability → responsibility map

| Capability | Responsibilities | Application | Domain | Infrastructure | Endpoints | Contracts |
| --- | --- | --- | --- | --- | --- | --- |
| **Cart** | create guest/authenticated cart | `CreateGuestCartCommand` | `ShoppingCart.CreateGuest` | `CartDirectory.CreateGuestAsync` | `POST /cart` | `CartSnapshot` |
| | read current authenticated cart | `GetCurrentAuthenticatedCartQuery` | — | `FindActiveAuthenticatedAsync` | `GET /cart/current` | `CartSnapshot` |
| | read cart by id (access-checked) | `GetCartQuery` | `ShoppingCart` | `GetCartAsync` | `GET /cart/{cartId}` | `ICartQueryGateway` |
| | merge guest cart after login | `MergeCartAfterLoginCommand` | `AdoptAuthenticatedOwner` | `MergeAnonymousAfterLoginAsync` | `POST /cart/merge` | `CartAccess` |
| | add / increase line | `AddCartLineCommand` | `ShoppingCart.AddLine`, `CartLine.Open` | `AddOrIncreaseLineAsync` | `POST /cart/{cartId}/lines` | `CartLineSnapshot` |
| | change line quantity (0 = remove) | `ChangeCartLineQuantityCommand` | `CartLine.ReplaceHold` | `ChangeLineQuantityAsync` | `PATCH …/lines/{lineId}` | — |
| | remove line | `RemoveCartLineCommand` | `ShoppingCart.RemoveLine` | `RemoveLineAsync` | `DELETE …/lines/{lineId}` | — |
| | expiry / abandon | (worker-driven, not HTTP) | `Expire`, `Abandon` | `ExpireDueCartsAsync`, `AbandonAsync` | — | `ICartExpiryReconciler` (Application) |
| | conversion seam | `CartConversionAdapter` | `MarkConverted` | `ConvertAsync` | — | `ICartConversionPort` |
| | presentation enrichment | `CartPresentationComposer` | — | (Catalog/Party Contracts) | — | `ICartPresentationGateway` |

## Secondary technical axes (target shape)

`Commands`, `Queries`, `Validators`, `Models`, `Ports` are **secondary** axes and must live under the
capability folder.

## Cross-capability shared concerns (legitimate)

| Folder | Concern | Why shared |
| --- | --- | --- |
| `Application/Validation/` | `CartFluentRules`, `CartValidationCodes` | used by 4 validators across Commands and Queries |
| `Application/Errors/` | `CartErrorCodes`, `CartExceptionMapper` | one module-wide fault vocabulary |
| `Application/Presentation/` | `CartPresentationComposer`, `CartCurrencyTotals` | one storefront projection |
| `Application/Composition/` *(new in W1)* | fault→Result composition | one module-wide seam, mirrors `Payment`/`AddressBook` |
| `Application/Lifetime/` | lifetime + persistence-policy ports | one lifetime capability |
| `Application/Ports/` | `ICartDirectory`, `ICartCommerceContextResolver` | implementation seams for the capability |
| `Application/Conversion/` | `CartConversionAdapter` | conversion seam (see W2 consolidation note) |

## Capability count and justification

```text
Capability count: 1  (Cart)
```

Per Structure skill §10, `TECHNICAL_AXIS_FIRST` is flagged when the Application tree is organized
primarily as `Commands/ | Queries/ | Validators/` **with per-use-case children**. Cart satisfies that
shape today (G5). Per §7 the hard requirement is capability-first regardless of capability count when
the module is being certified under `ARCH-COMPLETE-002`, and §8 makes a folder holding **one**
production source file `OVER_FOLDERED` when named after one use case. Cart has three such leaves
(`CreateGuestCart`, `MergeCartAfterLogin`, `GetCurrentCart`) plus two named-after-one-use-case leaves
holding request + validator only.

## Sibling precedent for the target shape

| Module | Shape | File count in capability |
| --- | --- | --- |
| `AddressBook` | `Application/Addresses/{Commands,Queries,Validators}/` | 4 / 2 / 5 |
| `Offer` | `Application/Offers/{Commands,Queries,Mappings,Policies,Ports,ReadModels}/` | 11 / 2 / 1 / 1 / 1 / 1 |
| `UserPreference` | `Application/LocalePreferences/{Commands,Queries,Validators}/` | 1 / 1 / 1 |
| `Wishlist` | `Application/Customer/{Commands,Queries,Validators}/` | 2 / 2 / 1 |
| `BulkInquiry` | `Application/Storefront/{Commands,Validators}/` | 1 / 1 |
| `Content` | `Application/Articles/{Commands,Queries,Models,Ports,Validators}/` | 6 / 7 / 1 / 2 / 3 |

Note that `UserPreference`, `Wishlist` and `BulkInquiry` keep a capability folder with a **single**
file in a technical axis (e.g. `LocalePreferences/Commands/UpsertUserPreferenceCommand.cs`). That is
capability-first-shallow: the leaf is the axis (`Commands`), not a per-use-case folder. Cart's defect
is the extra per-use-case level **plus** the technical axis sitting at the Application root.

## Modules that are NOT capability-first (recorded, out of scope)

`Notification`, `Wallet`, `Support`, `Returns`, `Settlement`, `CustomerProfile`, `Fulfillment`,
`Reviews` still use `Application/{Commands,Queries,Validators}/`. They are **not** part of this AMSC
run and are not evidence that Cart's current shape is acceptable — Cart is being certified under
`ARCH-COMPLETE-002` and the Architect explicitly requested professional VS-standard foldering.
