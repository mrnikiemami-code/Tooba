# Wishlist physical tree (before AMC-001)

```
Modules/Wishlist/
  Tooba.Wishlist.Domain/
    WishlistItem.cs                     # root aggregate; InvalidOperationException
  Tooba.Wishlist.Application/
    Ports/IWishlistDirectory.cs
    Models/WishlistModels.cs            # Catalog.Contracts card DTO
    Presentation/WishlistPresentationComposer.cs
    Validators/WishlistValidators.cs    # technical-axis validators + ad-hoc codes
    Commands/AddWishlistItem/…
    Commands/RemoveWishlistItem/…
    Queries/ListWishlistPage/…
    Queries/GetWishlistMembership/…
  Tooba.Wishlist.Contracts/
    IWishlistCountPort.cs               # root dump
    Errors/WishlistErrorCodes.cs
  Tooba.Wishlist.Infrastructure/
    WishlistModule.cs                   # + OutboxRegistration
    WishlistDirectory.cs
    Development/WishlistDevelopmentSeed.cs
    Persistence/WishlistDbContext.cs
    Migrations/…                        # root Migrations
  Tooba.Wishlist.Endpoints/
    WishlistEndpointModule.cs
    Customer/…                          # Results.Json
    Errors/ + Resources/
```

slnx: all five projects under flat `/Modules/` (not `/Modules/Wishlist/`).
