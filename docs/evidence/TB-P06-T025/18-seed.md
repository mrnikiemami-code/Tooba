# 18 — Development seed

Task: TB-P06-T025

## Gate

Seed runs **only** when `IsDevelopment` (same pattern as Reviews / ProductQnA).

## Idempotency

Skip insert when natural keys already present (stable demo ticket subjects / fixed UuidV7 seeds). Re-run safe.

## Contents (minimum)

| Item | Intent |
|------|--------|
| ≥2 customer tickets | different statuses + replies |
| ≥2 seller tickets | SellerPartyId scoped |
| ≥1 related Order | real demo Order id via Order gateway if available |
| ≥1 Admin public reply | triggers Notification to requester |
| Snapshot | `GET /v1/admin/support/demo-preview` returns concrete IDs |

## ID pattern

Prefer fixed UuidV7 constants documented in demo-preview payload (e.g. `support-demo-customer-open`, `support-demo-seller-waiting`) — exact values filled when Host seed lands.
