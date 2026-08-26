# 12 — Shipping address snapshot (TB-P06-T009)

## Immutable fields (copied at creation)

| Field | Source |
|---|---|
| `RecipientName` | Checkout |
| `ContactMobile` | Checkout |
| `ProvinceName` | Checkout |
| `CityName` | Checkout |
| `PostalAddress` | Checkout |
| `PostalCode` | Checkout |
| `ShippingMethodCode` | Checkout |
| `ShippingMethodLabel` | Checkout |

## Mechanism

- `FulfillmentUnit.CreateFromPaidOrder` copies trimmed checkout values into fulfillment row.
- No FK to AddressBook; no live lookup after creation.

## Test evidence

- Seed checkout with `"Snapshot Recipient"` / `"Original Address 1"`.
- After SQL update to checkout (`"Mutated Recipient"`, `"Mutated Address"`), fulfillment snapshot unchanged.

## Rationale

- Fulfillment ships to address confirmed at checkout/payment time.
- Later checkout edits do not retroactively change in-flight fulfillments.
