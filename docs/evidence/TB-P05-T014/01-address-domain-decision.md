# TB-P05-T014 — Address domain decision

## Ownership

Customer Address Book is a dedicated module (`Tooba.AddressBook`) because it owns a customer's private shipping/recipient book. It is not Product, Cart, Identity, or Party membership. Party remains organization/capability identity and does not store this shipping book.

The only entity is `CustomerAddress`, identified by `AddressId`, with server-authoritative `OwnerUserId`, recipient and contact fields, country/province/city, postal code and address line, optional building/unit and label, `IsDefault`, `CreatedAt`, and `UpdatedAt`.

## Invariants and identity

Owner identity is supplied by Host from `CurrentAuthenticatedSession`. Development/Testing may use the existing controlled customer actor seam (`X-Tooba-Dev-Actor-User-Id`, then `StorefrontGuestActorId`). Requests contain no `OwnerUserId`. Empty actor is rejected. Production without an authenticated actor returns 401 on `/v1/customer/addresses`.

At most one default address exists per owner. Setting a default atomically clears the previous default. Deleting the default leaves none; no automatic replacement is selected.

Validation is server-side and generic: recipient, bounded phone, country, city, and address line are required; postal code is length-bounded without Iran-only core rules. Missing country defaults to `IR`.

## Checkout snapshot

Checkout is not replaced. Optional `SavedAddressId` on `StorefrontCheckoutShippingInput` loads the actor's own row through `IAddressBookDirectory`. Foreign or missing ids fail with a clear ownership `InvalidOperationException`. Recipient/shipping scalar fields are copied onto `CheckoutGroup` / order snapshot. `AddressId` is not persisted on Order. Later AddressBook edits cannot rewrite a placed order.

Guest checkout without `SavedAddressId` keeps the existing inline shipping path and `StorefrontGuestActorId`.

## Boundaries

The module owns schema `address_book`, its migration, and Outbox table. There is no Order foreign key, no Catalog/Cart/Identity/Party navigation, and no cross-module SQL. Geocoding, shipping rates, and delivery zones are deferred.

## Deferred

Map picker, address SaaS validation, pickup network, fulfillment, save-from-checkout UI invention, and frontend Shopeiva binding beyond this backend capability.
