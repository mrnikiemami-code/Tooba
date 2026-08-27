# Customer profile (canonical)

- Owner: `CustomerProfile` module (`customer_profile` schema).
- Fields: DisplayName, FirstName, LastName, BirthDate, Bio.
- HTTP: `GET/PUT /v1/customer/profile` via `CustomerPanelEndpoints` (own actor only).
- Dev seed: `CustomerProfileDevelopmentSeed` for `StorefrontGuestActorId` (`aaaaaaaa-aaaa-4aaa-8aaa-000000000009`).
- Identity credentials are not stored here.
