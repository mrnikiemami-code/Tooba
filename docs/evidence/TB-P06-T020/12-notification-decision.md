# 12 — Notification decision (TB-P06-T020)

Date: 2026-08-27  
Gate: L — Notifications Decision Gate

## Audit

| Check | Result |
|---|---|
| `src/backend/Modules/**/Notification*` | **Absent** |
| Host `/v1/*/notifications` | **Absent** |
| Persistent inbox entity (recipient, IsRead, type, body) | **Absent** |
| Prior Wave 1 evidence | Deferred Host notification foundation |

Payment webhook “notification” DTOs are payment events, not customer/seller inbox.

## Decision

**Option C — DEFER** (`DEFERRED_WITH_REASON`)

Reasons:

1. No Notification module or Host owner to extend.
2. Minimum honest inbox (persistence + recipient isolation + mark-read + locale/tenant) exceeds safe Wave 2 commercial scope while Reviews list is the priority.
3. Implementing UI without backend would require fake unread badges / rows — disallowed.

## Consequences

| Surface | Action |
|---|---|
| Customer `/customer-panel/notifications` | Keep deferred shell; nav remains hidden |
| Seller notifications route | Not created; no Bell nav item |
| Fake push / realtime / unread | **Not implemented** |

## Verdict

**NOTIFICATIONS = DEFERRED_WITH_REASON**
