# 19 — Provider contract tests

**Task:** TB-P06-T022  
**Type:** `PaymentProviderContractTests` (+ related production policy tests)

## Harness intent

Any production adapter should pass these contract expectations. Current subject: `WebhookPaymentGateway` with example host `psp.example` (placeholder, not a commercial brand claim).

## Cases covered

| Case | Test / assertion |
|---|---|
| Initiation mapping | Reference `wh-…` + redirect to InitiateBaseUrl |
| Success verification | Status override succeeds even if callbackClaimsSuccess=false |
| Failure verification | Definitive `GATEWAY_REJECTED` not indeterminate |
| Unknown / pending | `GATEWAY_PENDING` is indeterminate |
| Invalid authenticity | Signature validator rejects bad HMAC |
| Unconfigured fail-closed | Initiate throws `payment.gateway.unconfigured` |
| SSRF / private host | Covered in `PaymentProductionPolicyTests` |
| Missing InitiateBaseUrl | Fail-closed even when StatusQuery set |
| Indeterminate code set | TIMEOUT / UNAVAILABLE / RATE_LIMITED / PENDING |

## Not claimed

Live HTTP calls to a real commercial PSP.  
Contract tests use in-memory overrides / example URLs only.

## Reuse

New provider adapters should extend this harness rather than invent ad-hoc proofs.
