# 15 — Realtime decision (TB-P06-T023)

## Decision

```text
REALTIME_NOTIFICATIONS = DEFERRED
```

## Audit

- No SignalR / realtime hubs found under `src/backend`
- No notification push channel or fake WebSocket poll invented
- Inbox refresh: navigation / explicit reload / client fetch on page load only

## Forbidden claims

```text
REALTIME_NOTIFICATIONS_LIVE = NOT CLAIMED
fake push = FORBIDDEN
```

Realtime is **not** required for Task PASS. Transactional persistence + poll-on-open is sufficient.
