# RECEIPTS — TB-STOREFRONT-GROUP-DELIVERY-CALENDAR-CLOSURE-251

## Identity

- Task: `TB-STOREFRONT-GROUP-DELIVERY-CALENDAR-CLOSURE-251`
- Bridge row: `23852239-7fad-4892-858f-894803f66854`
- Base-SHA: `7d916bc7a8154f77ff2ee734f0d78c6ca54459cd`
- ClaimedAtUtc: `2026-11-03T14:39:13.8963523`
- Channel: `tooba-main` / Worker: `tooba-worker-01`

## Per-group calendar (Active `01a1a1b2-0c5a-7000-8b82-2cc2e119dbe9`)

| groupKey | seller | method | snapshot | dates | slots/day |
|----------|--------|--------|----------|-------|-----------|
| cap\|platform\|store\|01a030d1…\|01a16f43… | آرمان | پیشتاز post/null | 01a186f7-dd7a… | 7 | 8 |
| cap\|platform\|store\|01a0ffff…\|01a16f43… | نیلوا | پیشتاز post/null | 01a186f7-dd7a… | 7 | 8 |

Host CapturedPreview: local env ON; checked-in OFF. Projection `hasCapturedPreview=true`.

## Tests

| Suite | Result |
|-------|--------|
| Focused shipping calendar/continue/selection/preview/money | 79 passed (aggregate from worker) |
| calendar-251 + cart-money residual | 17 passed |
| cart-mount | 10 passed |
| typecheck / build | 0 / 0 |

## Canonical

- Host PID **23472** :5088/health 200
- FE PID **7632** :3000/fa 200
- Isolated CDP9371 shipping DOM: 2 groups independent calendars; silent=false
- DataProtection preserved; mutations none; migrations none

## Code-SHA / Evidence-SHA

- Code-SHA: 1ed7800bedc1f16e67e6a81e9d84c4b2e8251ecd
- Evidence-SHA: EVIDENCE_SHA_PENDING
