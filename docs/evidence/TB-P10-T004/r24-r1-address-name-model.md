# TB-P10-T004-R24-R1 — Address name model

Canonical new fields: `FirstName`, `LastName` (UI نام / نام خانوادگی).

Persisted independently on:

- `address_book.customer_addresses.first_name` / `last_name`
- `order.cart_shipping_drafts.first_name` / `last_name`
- `order.checkouts.recipient_first_name` / `recipient_last_name`

`RecipientName` remains composed display (`First + Last`) for new rows and the committed snapshot.

Legacy rows with only `RecipientName`: first/last empty; UI shows `RecipientName`; no guessed split.

New address create requires both names. FA labels; EN smoke via locale copy on account menu. Validation messages are Persian, not raw codes.
