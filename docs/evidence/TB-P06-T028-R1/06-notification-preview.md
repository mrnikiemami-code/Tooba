# 06 — Notification preview

## Inbox URL

```text
http://localhost:3000/customer-panel/notifications
```

## Seeded rows to inspect (latest first)

| Title | Body (sample) |
| --- | --- |
| بازگشت وجه موفق | بازگشت وجه مرجوعی با موفقیت انجام شد. |
| تأیید مرجوعی | درخواست مرجوعی تأیید شد. |
| بازگشت به کیف پول / پرداخت با کیف پول | پرداخت به مبلغ … IRR با موفقیت انجام شد. |

## Deep-link

No dedicated per-notification detail route is required for preview. Inbox lists live Host notifications for actor `aaaaaaaa-aaaa-4aaa-8aaa-000000000009`. Open inbox URL after refund scenario; top rows show refund success + wallet payment events.

Capture: `captures/04-notifications.png`
