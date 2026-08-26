# 01 — Runtime before session hardening (TB-P06-T007)

## Predecessor

| Field | Value |
|---|---|
| Task | TB-P06-T006 |
| Commit | `9c6b5d2e981022def294db0bef2bc42e1d93be9e` |
| Branch | `main` |
| Bridge UUID | `ed6c8d78-bcb6-4b5d-995d-95c2b18b7358` |

## Services started

| Service | Port / URL | Status |
|---|---|---|
| PostgreSQL | local dev instance | running |
| Backend (`Tooba.Host`) | `http://127.0.0.1:5088` | running |
| Frontend (Next.js) | `http://127.0.0.1:3001` | running |

## Smoke checks (before TB-P06-T007 changes)

| Check | URL | Expected | Result |
|---|---|---|---|
| Liveness | `GET /health/live` | 200 | 200 |
| Readiness | `GET /health/ready` | 200 | 200 |
| Storefront Home | `GET /` (frontend) | 200 | 200 |
| Host auth | `POST /v1/auth/login` | Bearer JSON response | 200/401 |

## Notes

- Host Bearer SessionId auth live; no BFF auth routes yet.
- Customer/storefront clients called Host directly or used localStorage session tokens.
- OTP via legacy `CapturingOtpSender` / `ProductionOtpSender`; no `IOtpDeliveryProvider`.
