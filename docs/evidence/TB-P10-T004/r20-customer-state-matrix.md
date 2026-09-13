# R20 customer state matrix

| State | How | Browser / API |
| --- | --- | --- |
| A Active Cycle #1 | Commit TB-…bb5f36 | FA cart: countdown + «پرداخت» + hold copy |
| B Fail same cycle | Sandbox fail while hold active | FA cart: «پرداخت ناموفق بود؛ تا پایان مهلت رزرو می‌توانید دوباره تلاش کنید.» + countdown `00:06` + Pay |
| C Expired | Hold reached zero | «مهلت رزرو موجودی پایان یافته است.» + retry CTA |
| D Retry Cycle #2 | Retry after expiry | «مهلت رزرو مجدد» + new countdown + Pay |
| E Unavailable after expiry | `on_hand=0` then unpaid-retry, then restored | HTTP 409 `payment.unpaid.supply_unavailable` / «این سفارش در حال حاضر قابل تأمین نیست.» |
| F Max cycles | max=2 after cycle #2 ended | «تعداد دفعات مجاز رزرو مجدد موجودی برای این سفارش به پایان رسیده است.» action=none |
| G Manual AwaitingAdmin | provider=manual + evidence | «در انتظار بررسی پرداخت» + review hold countdown, no Pay |
| H Success removes card | sandbox success TB-…267a36 | Paid checkout omitted from pending list |
| I New cart + old pending | new guest cart + proofs | Active line + pending cards |

Copy is Persian-natural; raw `PostgresException` no longer shown on cards.
