# TB-TMAR-WISHLIST-AMC-001-W3 — CQRS / Result / catalog

## Outcome

Customer wishlist HTTP dispatches through MediatR `ISender` → Application handlers → `WishlistOperation` (`SemanticException` → `Result`) → `ApiResponseFactory.From` / `Created`. Zero `Results.Json` in Endpoints. Zero endpoint `catch (SemanticException)`.

## Inventory

| Request | Validator | Classification |
|---|---|---|
| `AddWishlistItemCommand` | `AddWishlistItemCommandValidator` | VALIDATOR_REQUIRED_PRESENT |
| `RemoveWishlistItemCommand` | `RemoveWishlistItemCommandValidator` | VALIDATOR_REQUIRED_PRESENT |
| `GetWishlistMembershipQuery` | `GetWishlistMembershipQueryValidator` | VALIDATOR_REQUIRED_PRESENT |
| `ListWishlistPageQuery` | — | VALIDATOR_NOT_REQUIRED (Actor-only trust boundary) |

## Error ownership

- Catalog/resource set/resx: `Tooba.Wishlist.Contracts`
- Registration: `WishlistModule` (Infrastructure)
- Endpoints Errors/Resources: ABSENT
- Validator codes live in `WishlistErrorCodes` and catalog descriptors
- `customer.session.required` remains Foundation-owned (not re-registered)

## Coupling

Zero foreign Application/Infrastructure/Domain. Catalog lookup remains `Catalog.Contracts` only. CustomerProfile consumes `IWishlistCountPort` via Contracts.Ports only.
