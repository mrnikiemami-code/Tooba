# Evidence — TB-P06-T023

**Transactional Notifications — Real Customer/Seller Notification Inbox from Commerce Events**

| Field | Value |
|---|---|
| Task-ID | `TB-P06-T023` |
| Phase | P06 |
| Channel | `tooba-main` |
| Predecessor | `852c35fcef809c911ffe3a1c4f290f0cda486fe7` |
| Commit message target | `feat add transactional notifications [TB-P06-T023]` |
| Architect status (SoT) | `AWAITING_ARCHITECT_ACCEPT` (Worker complete; not Architect ACCEPT) |

## Allowed claims

```text
TRANSACTIONAL_NOTIFICATIONS_LIVE = YES
NOTIFICATION_BACKEND = LIVE
CUSTOMER_NOTIFICATION_UI = LIVE
SELLER_NOTIFICATION_UI = LIVE
NOTIFICATION_UNREAD = LIVE
NOTIFICATION_DEEP_LINKS = LIVE
REALTIME_NOTIFICATIONS = DEFERRED
FAKE_NOTIFICATIONS = FORBIDDEN
SELLABLE_DEMO = YES
```

## Must NOT claim

```text
REALTIME_NOTIFICATIONS_LIVE
PRODUCTION_GO_LIVE_READY
USER_VISUAL_ACCEPTED
PRODUCT_FULLY_READY
```

## Validation summary (Worker Result)

| Check | Result |
|---|---|
| Host.Tests | **269 passed**, 0 failed, 0 skipped |
| FE lint + tsc | green |
| E2E sandbox payment | customer: `payment.succeeded`, `fulfillment.created`; seller: `order.paid.seller`, `fulfillment.created`; mark-read works |
| Screenshots | `captures/01-customer-notifications.png`, `captures/02-seller-notifications.png` |
| API dump | `e2e-notification-api.json` |
| Browser manifest | `browser-proof.json` |

## Capability summary

| Area | Fact |
|---|---|
| Module | `src/backend/Modules/Notification/` (Domain / Application / Infrastructure) |
| Schema | `notification` |
| Host APIs | `/v1/customer\|seller/notifications` (+ unread-count, `{id}/read`, read-all, DELETE) |
| Transport | MassTransit PostgreSQL SQL + outbox — **NO RabbitMQ** |
| Consumers | payment.succeeded/failed, fulfillment.created, shipment.dispatched, return.requested/approved, refund.succeeded |
| Story | **DEFERRED** (no invented events) |
| FE | `notification-inbox.tsx` ports Shopeiva `notifications.jsx` (`#E53935`, filters, mark read/delete) |
| Nav | Live in `customer-panel-shell` + `vendor-shell`; removed from `CUSTOMER_DEFERRED` |
| Toast | Inline flash (no `react-toastify`) — minor technical deviation |
| Empty inbox | Honest until commerce events |
| Realtime | **DEFERRED** — poll-on-navigation only |

## Files

| # | File | Topic |
|---|---|---|
| 01 | `01-runtime-before-notifications.md` | Pre-work runtime triad |
| 02 | `02-shopeiva-notification-source-map.md` | Shopeiva source lock |
| 03 | `03-commerce-event-audit.md` | Event availability |
| 04 | `04-notification-domain.md` | Domain model |
| 04b | `04-backend-structure.md` | Module/host structure (extra) |
| 05 | `05-notification-localization.md` | fa/en resolve-at-read |
| 06 | `06-notification-event-consumers.md` | MassTransit handlers |
| 07 | `07-recipient-resolution.md` | Order bridge recipients |
| 08 | `08-customer-notification-api.md` | Customer HTTP API |
| 09 | `09-seller-notification-api.md` | Seller HTTP API |
| 10 | `10-customer-notification-ui.md` | Customer inbox UI |
| 11 | `11-seller-notification-ui.md` | Seller inbox UI |
| 12 | `12-navigation-integrity.md` | Live nav / no dead links |
| 13 | `13-notification-target-safety.md` | Allowlisted deep links |
| 14 | `14-read-unread-semantics.md` | Read/unread rules |
| 15 | `15-realtime-decision.md` | `REALTIME = DEFERRED` |
| 16 | `16-notification-observability.md` | Safe counters |
| 17 | `17-notification-e2e.md` | E2E scenarios + sandbox proof |
| 18 | `18-browser-proof.md` | Screenshots + manifest |
| 19 | `19-visual-fidelity.md` | Shopeiva UI lock |
| 20 | `20-notification-authorization.md` | Auth isolation |
| 21 | `21-notification-tests.md` | Test evidence |
| 22 | `22-final-validation.md` | Validation gate (269 Host.Tests) |
| 23 | `23-final-runtime.md` | Runtime + preview URLs |
| 24 | `24-commercial-readiness.md` | Commercial matrix |

## Key implementation notes

- Idempotency: unique `(recipient_kind, recipient_party_id, source_event_id)`.
- Recipients from `IOrderNotificationReader` / `OrderNotificationBridge` — never from public payloads.
- Locale: persist `Type` + payload; resolve copy in `NotificationCopy.Resolve` at list time.
- Realtime / push intentionally absent; poll-on-navigation only.
