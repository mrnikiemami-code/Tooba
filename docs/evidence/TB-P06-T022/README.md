# Evidence — TB-P06-T022

**Production Payment Readiness — Harden Real PSP Adapter Boundary**

| Field | Value |
|---|---|
| Task-ID | `TB-P06-T022` |
| Phase | P06 |
| Channel | `tooba-main` |
| Predecessor | `f9c20bb8377f053050b64142d59e20cd53aed833` |
| Commit message target | `feat harden production payment readiness [TB-P06-T022]` |
| Architect status (SoT) | `AWAITING_ARCHITECT_ACCEPT` |

## Allowed claims

```text
PRODUCTION_PAYMENT_FOUNDATION_READY = YES
REAL_PSP_PROVIDER_CONFIGURATION_REQUIRED = YES
```

## Must NOT claim

```text
REAL_BANK_PAYMENT_PROVEN
PRODUCTION_GO_LIVE_READY
USER_VISUAL_ACCEPTED
PRODUCT_FULLY_READY
```

## Discovery result

```text
NO_PRODUCTION_PROVIDER_TARGET
```

No commercial PSP brand selected or hardcoded. Production `Mode` remains `Disabled` (fail-closed) until external provider URLs + secrets are supplied.

## Capability flags (honest)

```text
PAYMENT_FOUNDATION = LIVE
PAYMENT_SANDBOX = LIVE (Development FakePaymentGateway)
PAYMENT_CALLBACK_VERIFICATION = LIVE (HMAC + StatusQuery)
PAYMENT_RECONCILIATION = LIVE (background + admin reconcile)
PAYMENT_IDEMPOTENCY = LIVE
PAYMENT_PRODUCTION_ADAPTER = READY_FOR_PROVIDER (WebhookPaymentGateway boundary)
REAL_PSP_CONFIG = REQUIRED
REAL_BANK_PAYMENT = NOT_PROVEN
VISUAL_CONTRACT = SHOPEIVA_LOCKED
SELLABLE_DEMO = YES
PRODUCTION_GO_LIVE_READY = NO
```

## Files

| # | File | Topic |
|---|---|---|
| 01 | `01-runtime-before-payment-readiness.md` | Pre-work runtime triad |
| 02 | `02-payment-architecture-audit.md` | Architecture classification |
| 03 | `03-provider-configuration-discovery.md` | `NO_PRODUCTION_PROVIDER_TARGET` |
| 04 | `04-production-provider-contract.md` | `IPaymentGateway` contract |
| 05 | `05-provider-secure-configuration.md` | Secrets + fail-closed |
| 06 | `06-payment-state-machine.md` | Aggregate transitions |
| 07 | `07-callback-authenticity.md` | HMAC webhook authenticity |
| 08 | `08-payment-amount-integrity.md` | Amount/currency checks |
| 09 | `09-payment-idempotency.md` | Duplicate-safe paths |
| 10 | `10-payment-unknown-outcome.md` | Indeterminate → Pending |
| 11 | `11-payment-reconciliation.md` | Background + admin reconcile |
| 12 | `12-payment-outbox-events.md` | MassTransit / outbox |
| 13 | `13-payment-refund-compatibility.md` | Refund contract fail-closed |
| 14 | `14-admin-payment-operations.md` | Admin inspect + reconcile |
| 15 | `15-customer-payment-truthfulness.md` | Customer UX truth |
| 16 | `16-payment-observability.md` | Metrics (no secrets) |
| 17 | `17-payment-security-tests.md` | Security / SSRF tests |
| 18 | `18-real-provider-adapter.md` | Boundary ready; config required |
| 19 | `19-provider-contract-tests.md` | Contract harness |
| 20 | `20-payment-e2e-scenarios.md` | Scenario map |
| 21 | `21-payment-browser-proof.md` | Capture may be deferred |
| 22 | `22-visual-regression-audit.md` | UI lock |
| 23 | `23-payment-authorization-isolation.md` | Auth isolation |
| 24 | `24-final-validation.md` | Validation placeholders |
| 25 | `25-user-preview.md` | Preview URLs |
| 26 | `26-commercial-readiness.md` | Commercial matrix |

## Key implementation notes

- `WebhookPaymentGateway` requires `InitiateBaseUrl` + `StatusQueryBaseUrl` + `WebhookSigningSecret`.
- SSRF: `AllowedStatusQueryHosts` allowlist; private/loopback blocked by default.
- `VerifyMaxAttempts` bounded retry for indeterminate StatusQuery results.
- `PaymentDirectory` does **not** `ApplyVerifiedFailure` for `GATEWAY_TIMEOUT` / `UNAVAILABLE` / `RATE_LIMITED` / `PENDING`.
- Admin: `GET /v1/admin/payments/{id}`, `POST …/reconcile`; order detail embeds payment ops; FE uses existing `Info` fields (no redesign).
