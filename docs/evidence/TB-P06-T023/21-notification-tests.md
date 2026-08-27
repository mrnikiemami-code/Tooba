# 21 — Notification tests (TB-P06-T023)

## Backend

| Suite | Coverage | Result (reported) |
|---|---|---|
| `NotificationFoundationTests` | Schema `notification`, endpoints present, allowlist rejects `javascript:` / `/admin`, idempotent create, mark-read, cross-seller isolation, payment→project path | **3 passed** |

File: `src/backend/Host/Tooba.Host.Tests/NotificationFoundationTests.cs`

## Frontend

| Suite | Coverage | Result (reported) |
|---|---|---|
| `customer-panel/panel-nav-integrity.test.ts` | notifications live; removed from deferred | part of **4 nav integrity passed** |
| `vendor-panel/panel-nav-integrity.test.ts` | notifications live | part of **4 nav integrity passed** |

## Scenario ↔ test mapping

| Requirement | Covered by |
|---|---|
| Duplicate event suppressed | Foundation idempotency test |
| Unread / mark-read idempotent | Foundation |
| Cross-seller isolation | Foundation |
| Locale response | `NotificationCopy.Resolve` used in list path (fa default / en query) |
| Story review | **Not wired** — no test inventing story events |
| MassTransit SQL / outbox | Architectural (Host transport + module handlers); no RabbitMQ |

## Not claimed

Full Host.Tests green matrix belongs in `22-final-validation.md` when full `dotnet test` / `npm` runs complete for Result.
