# 01 — Runtime before payment hardening (TB-P06-T008)

| Service | URL | Status |
|---|---|---|
| PostgreSQL | local docker | available |
| Backend (`Tooba.Host`) | `http://localhost:5088` | baseline pre-change |
| Frontend (Next.js) | `http://localhost:3000` | baseline pre-change |

Pre-task payment baseline: `FakePaymentGateway` always registered; `StorefrontPaymentComposer` hardcoded provider `"fake"`; no webhook endpoint; no reconciliation worker; no `payment.webhook_inbox` table.
