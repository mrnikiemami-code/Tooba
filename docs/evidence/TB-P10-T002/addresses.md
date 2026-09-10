# Addresses

- Auth: saved addresses via existing AddressBook BFF (`listCheckoutSavedAddresses` / `createCustomerAddress`).
- Guest: checkout-scoped inline address; no invented account address book.
- Province/city from Host `GET /v1/storefront/geography/provinces` (not FE hardcode).
- Recipient prefills from saved/new address; SavedAddressId ownership enforced on prepare.
