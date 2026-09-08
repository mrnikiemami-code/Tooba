# T009-R1 Runtime smoke

Host `:5088` `Host: alpha.localhost` health 200. DevActor `01a036c2-970e-7000-8eb7-94bf5cc2d8db`.

## Safely restorable (no payout)

Checkout `01a07c4a-2344-7000-ad84-a20f4d31d392`

- GET: `confirm_deposit,reject_deposit,cancel`
- POST cancel 200 (3 sellers)
- GET: `restore_cancelled_order` projected
- POST `restore_cancelled_order` 200

## Completed seller payout blocks restore

Local SingleStore has no marketplace accrual. Fixture via Settlement schema APIs/tables (no payout rewrite):

- Credit `019a0000-0000-7000-8000-00000000ee01` net 900 on seller order `01a07c4a-235e-…`
- Succeeded payout `019a0000-0000-7000-8000-00000000cc01` amount 900

Then cancel same checkout:

- GET: `confirm_deposit,reject_deposit` — **no** `restore_cancelled_order`
- POST `restore_cancelled_order` 400 `order.restore.seller_payout_completed`
- FA: این سفارش به‌دلیل انجام تسویه/واریز سهم فروشنده قابل بازگردانی نیست.
- Payout row unchanged (`Succeeded`, 900, `updated_at` 2026-09-08 00:02:00+00)
- Credit row unchanged

## Existing blockers

Delivered `01a07bd2-5ef4-7000-b770-a2173d8ae25c`: GET `request_return` only. POST restore 400 `order.restore.not_cancelled`.
