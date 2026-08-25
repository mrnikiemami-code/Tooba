# TB-P05-T014 — Guest checkout regression proof

## Runtime

Verified on 2026-08-25 with the normal Development Host at `http://127.0.0.1:5088`. Guest cart was created without an actor header, one taxed `workspace-live-shirt` Offer line was added with `X-Tooba-Guest-Secret`, and checkout was submitted without `savedAddressId`.

## Result

Host returned `200` with inline shipping recipient `مهمان خطی`. Placement used the existing guest path (`StorefrontGuestActorId` when no saved address is selected). No AddressBook lookup was required.

## Contract retained

- Inline shipping validation still rejects empty recipient/contact/city/address fields (Host unit test `Guest_inline_checkout_keeps_existing_shipping_validation`).
- Checkout UI hides the saved-address block when `GET /v1/customer/addresses` is unauthorized and keeps the existing inline form.
- There is no save-from-checkout control.

## Correlation

`07` / `11` show the authenticated saved-address block. Guest regression is API-proven here so inventory-consuming guest submits do not need a separate browser capture after the authenticated evidence.
