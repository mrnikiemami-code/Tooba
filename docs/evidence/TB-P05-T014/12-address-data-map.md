# TB-P05-T014 — Address data map

## Owned persistence

AddressBook owns only schema `address_book`:

- `customer_addresses`: `AddressId`, server-derived `OwnerUserId`, `RecipientName`, `ContactMobile`, `Country`, `ProvinceName`, `CityName`, `PostalCode`, `PostalAddress`, optional `BuildingUnit` / `Label`, `IsDefault`, `CreatedAt`, `UpdatedAt`.
- Unique/default constraints enforce at most one default per owner.
- Module-local Outbox table.

There is no Order foreign key, Catalog/Cart/Identity/Party navigation, price, stock, or geocoding column.

## Write path

1. Host resolves the current actor from `CurrentAuthenticatedSession`; only Development/Testing permits the controlled actor header, otherwise Production missing actor is `401`.
2. Create/update bodies accept only shipping/recipient fields — never `OwnerUserId`.
3. `IAddressBookDirectory` scopes every mutation by the resolved actor.
4. Setting default clears any previous default for that owner atomically.

## Read and checkout composition path

1. `IAddressBookDirectory.ListAsync(actorUserId)` returns only the actor's owned rows.
2. Customer panel maps those rows into Shopeiva cards.
3. Checkout optionally receives `SavedAddressId`. Host loads the owned row and copies scalar shipping/recipient fields into `CheckoutGroup` / Order snapshot columns already owned by Order.
4. `AddressId` is not persisted on Order. Later AddressBook edits cannot rewrite placed orders.

## Live response example

Seeded default address for the Development customer actor:

- label `خانه`
- recipient `گیرندهٔ نمایشی توبا`
- mobile `+989120000014`
- city/province `تهران` / `تهران`
- postal address `خیابان نمونه، پلاک ۱۴، دفتر نمایشی فروشگاه`
- `isDefault: true`

These fields are rendered in `03`, `05`, `07`, `10`, and `11` and snapshotted by checkout in `08`.
