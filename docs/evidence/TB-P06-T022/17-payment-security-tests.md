# 17 — Payment security tests

**Task:** TB-P06-T022

## Test surfaces

| Concern | Coverage |
|---|---|
| Forged / tampered callback | HMAC validator rejects bad signature |
| Amount / currency tampering | Webhook amount_mismatch path (foundation + ops docs) |
| Provider reference mismatch | Attempt mismatch guards |
| Fail-closed Production | `FailClosedPaymentGateway` + unconfigured Webhook initiate |
| SSRF | Private/loopback StatusQuery host blocked without allowlist |
| Initiate requires full config | Missing InitiateBaseUrl fails closed even if StatusQuery set |
| Indeterminate vs definitive | Theory tests on `PaymentGatewayOutcomes` |
| Admin auth | Admin endpoints require `AdminPanelAccess` |
| Open redirect | Initiate redirect built only from configured `InitiateBaseUrl` (not free-form client URL) |
| Cross-tenant / foreign customer | Payment `GetAsync` actor visibility guard (foundation) |
| Seller mutation of payment | No seller payment-state mutation API in this Task |

## Primary test types

- `PaymentProductionPolicyTests`
- `PaymentProviderContractTests`
- Existing `PaymentFoundationTests` (forged callback, idempotency, multi-seller)

## Honest claim

Security foundations and new production-policy/contract tests are in-repo.  
Fill exact pass counts in `24-final-validation.md` after `dotnet test` on the worker run. No secret values are recorded here.
