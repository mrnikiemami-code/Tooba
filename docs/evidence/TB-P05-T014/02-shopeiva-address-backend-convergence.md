# TB-P05-T014 — Shopeiva address/backend convergence (backend start)

## Shopeiva feature

Shopeiva already contains customer address cards/forms and checkout address selection. Earlier Customer Panel left Addresses as an honest shell because backend capability did not exist.

## Backend capability before

No owning AddressBook module. Dashboard exposed `AddressBookAvailable: false`. Checkout accepted only inline shipping fields and always placed as `StorefrontGuestActorId`.

## Backend capability after

Dedicated AddressBook module provides private create/list/get/update/delete/set-default, one-default invariant, schema/migration/Outbox, Development seed, and Host HTTP under `/v1/customer/addresses`. Checkout may optionally snapshot an owned saved address without storing `AddressId` on Order.

## Binding path

- Actor is resolved server-side with the same seam as `CustomerPanelEndpoints`.
- `IAddressBookDirectory` is the application contract; Host maps HTTP and checkout composition.
- `StorefrontCheckoutComposer.SubmitAsync` copies recipient/phone/province/city/address/postal code from AddressBook when `SavedAddressId` is present.

## Checkout integration

- Saved address selection is ownership-checked.
- Historical order uses immutable shipping/recipient snapshot columns already owned by Order.
- Guest inline checkout remains the default when `SavedAddressId` is absent.

## Minimal UI additions

None in this backend slice. Dashboard now reports `AddressBookAvailable: true`. Frontend live binding is recorded under Completed live frontend convergence below.

## Deferred capability

Geocoding, shipping-rate, delivery-zone, map picker, and save-from-checkout (Shopeiva has no such control).

## Completed live frontend convergence

- Customer panel `/customer-panel/addresses` replaces the honest shell with live list/create/edit/default/delete, empty copy «هیچ آدرسی یافت نشد», and 401/403 account required. Cards keep Shopeiva language (پیش‌فرض / ویرایش / حذف / آدرس جدید) in Tooba blue `#2563EB`. Search, type-filter, delivery times, order counts, map picker, and save-from-checkout were not ported.
- Checkout adds the Shopeiva «انتخاب آدرس» block (`آدرس جدید` / `آدرس‌های من`) only when `GET /v1/customer/addresses` succeeds. Selecting a card fills inline shipping fields and submit includes `shipping.savedAddressId`. «آدرس جدید» clears that id. 401/403 hides the block and keeps the existing inline form. There is no save-from-checkout checkbox.
- Binding path: `customer-address-api.ts` reuses `customerAuthHeaders` (`Bearer` or `X-Tooba-Dev-Actor-User-Id`). List mapping accepts camelCase/PascalCase arrays or `items`/`addresses` envelopes. Write payload is `recipientName`, `contactMobile`, `country`, `provinceName`, `cityName`, `postalCode`, `postalAddress`, optional `buildingUnit`/`label`, and `isDefault`.
- Dashboard mapper reads `addressBookAvailable: true` and live `addressBookCount` from Host.
- Screenshots `03`–`07`, `10`–`11` and proofs `08`, `09`, `12`–`14` are under `docs/evidence/TB-P05-T014/`.

## Frontend binding path (detail)

- `GET/POST /v1/customer/addresses`
- `PUT/DELETE /v1/customer/addresses/{addressId}`
- `POST /v1/customer/addresses/{addressId}/default`
- `POST /v1/storefront/checkout` shipping may include `savedAddressId`; guest omits it.
