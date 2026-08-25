# TB-P05-T014 — Checkout saved-address snapshot proof

## Runtime

Verified on 2026-08-25 against the normal Development Host at `http://127.0.0.1:5088` and the normal Next application at `http://127.0.0.1:3000`. Checkout used a taxed `workspace-live-shirt` Offer so Host tax settlement was available.

## Own saved address

Actor `aaaaaaaa-aaaa-4aaa-8aaa-000000000009` submitted checkout with `shipping.savedAddressId = aaaaaaaa-aaaa-4aaa-8aaa-0000000000a1`. Host returned `200` and copied AddressBook fields onto the Checkout/Order shipping snapshot:

- `recipientName`: `گیرندهٔ نمایشی توبا`
- `postalAddress`: `خیابان نمونه، پلاک ۱۴، دفتر نمایشی فروشگاه`

`SavedAddressId` is not a field on `CheckoutGroup` / Order persistence. The response carries only the copied scalars.

## Foreign AddressId

Actor `cccccccc-cccc-4ccc-8ccc-000000000014` attempted the same saved AddressId and received:

```json
{"title":"Forbidden","errorCode":"checkout.address.forbidden","detail":"این نشانی متعلق به مشتری جاری نیست."}
```

Status `403`. Host mapping prefers ownership language over the generic missing-checkout branch.

## Historical immutability

After the successful snapshot checkout, AddressBook default row was PUT to recipient `ویرایش‌شده بعد از سفارش` / address `آدرس تغییر کرده بعد از سفارش`. Reloading the placed checkout still returned `گیرندهٔ نمایشی توبا`. AddressBook was then restored to the deterministic seed copy. Mutable AddressBook therefore cannot rewrite a placed order snapshot.

## UI correlation

`07-checkout-saved-address-selection.png` and `11-checkout-mobile-address-390x844.png` show the live «آدرس‌های من» cards that feed this snapshot path.
