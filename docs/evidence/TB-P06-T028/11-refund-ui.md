# 11 — Refund-to-wallet UI

Task: TB-P06-T028 (frontend)

## Destination model (typed)

| Value | Persian label | Default |
|-------|---------------|---------|
| `OriginalPayment` | پرداخت اصلی | **yes** |
| `Wallet` | کیف پول | |

No free-form destination strings.

## Surfaces

| Surface | Behavior |
|---------|----------|
| Customer return form (`ReturnFormModal`) | Destination selector; create body includes `destination` |
| Seller approve (`ReturnReviewModal`) | Destination selector; approve body `{ destination }` |
| Detail card | Shows selected destination |

## API wiring

- `createCustomerReturn({ …, destination })` → `POST /api/customer/returns`
- `sellerApproveReturn(party, id, destination)` → `POST /v1/seller/returns/{id}/approve`
- `mapReturnSnapshot` reads `destination` / `Destination` (defaults `OriginalPayment`)

## Files

- `app/returns/return-api.ts` (+ tests)
- `app/returns/return-ui.tsx`

## Ledger labels (history)

Verified in `formatLedgerEntryLabel`:

- `OrderPaymentDebit` → «پرداخت سفارش»
- `RefundCredit` → «اعتبار مرجوعی»
- `GiftCardCredit` → «اعتبار کارت هدیه»
