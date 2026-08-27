# 01 — Runtime before notifications (TB-P06-T023)

## Recovery

```text
branch: main
HEAD: 852c35fcef809c911ffe3a1c4f290f0cda486fe7
origin/main: 852c35fcef809c911ffe3a1c4f290f0cda486fe7
predecessor match: YES
```

## Runtime

| Probe | Result |
|---|---|
| Host `/health` | 200 |
| Host `/health/ready` | 200 |
| FE customer-panel | 200 |
| FE vendor-panel | 200 |
| Shopeiva `:3001` | 200 |

## Pre-state

- No Notification module
- Customer `/customer-panel/notifications` = honest unavailable stub (deferred nav)
- Seller notifications route absent
- REALTIME not present
