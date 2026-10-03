# TB-TMAR-WISHLIST-AMC-001-W2 — Layer structure

## Physical layout (after)

```text
Modules/Wishlist/
  Tooba.Wishlist.Domain/
    Aggregates/WishlistItem.cs
  Tooba.Wishlist.Application/
    Ports/IWishlistDirectory.cs
    Models/WishlistModels.cs
    Composition/WishlistPresentationComposer.cs
    Customer/
      Commands/{Add,Remove}WishlistItemCommand.cs
      Queries/{ListWishlistPage,GetWishlistMembership}Query.cs
      Validators/WishlistValidators.cs
  Tooba.Wishlist.Contracts/
    Ports/IWishlistCountPort.cs
    Errors/WishlistErrorCodes.cs
  Tooba.Wishlist.Infrastructure/
    WishlistModule.cs
    Directories/WishlistDirectory.cs
    Persistence/{WishlistDbContext.cs,Migrations/*}
    Development/WishlistDevelopmentSeed.cs
  Tooba.Wishlist.Endpoints/  (HTTP surface; catalog/Result hardening deferred to W3)
```

## Path ↔ namespace

- `Domain/Aggregates` → `Tooba.Wishlist.Domain.Aggregates`
- `Application/Customer/*` → `Tooba.Wishlist.Application.Customer.*`
- `Application/Composition` → `Tooba.Wishlist.Application.Composition`
- `Contracts/Ports` → `Tooba.Wishlist.Contracts.Ports`
- `Infrastructure/Directories` → `Tooba.Wishlist.Infrastructure.Directories`
- `Infrastructure/Persistence/Migrations` → `Tooba.Wishlist.Infrastructure.Persistence.Migrations`

## Coupling (unchanged policy)

- Foreign App/Infra/Domain: **ZERO**
- Allowed Contracts: Catalog.Contracts (+ Order.Contracts.Fulfillment session seam in Development seed)
- CustomerProfile consumes `IWishlistCountPort` via `Wishlist.Contracts.Ports` only

## Deferred to W3

- Endpoints `Results.Json` → `ApiResponseFactory` / Result
- Directory/Domain `InvalidOperationException` → SemanticException where appropriate
- Error catalog + resx under Contracts (or Endpoints per certify pattern)
- Overbox registration extract if Structure skill requires Persistence leaf
