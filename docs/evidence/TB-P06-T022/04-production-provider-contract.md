# 04 — Production provider contract

**Task:** TB-P06-T022

## Canonical abstraction

`IPaymentGateway` + `IPaymentRefundGateway` in Payment Application contracts.

| Capability | Contract surface | Production boundary behavior |
|---|---|---|
| Create / Initiate | `InitiateAsync(paymentId, amount, currency)` | Requires full Webhook config; builds redirect to `InitiateBaseUrl` |
| Redirect / payment URL | `GatewayInitiation.RedirectUrl` | Query includes paymentId, amount, currency, reference, returnPath |
| Provider reference | `ProviderRequestReference` | `wh-{paymentId:N}` for webhook adapter |
| Verify | `VerifyAsync(reference, callbackClaimsSuccess)` | **Ignores** callbackClaimsSuccess; StatusQuery decides |
| Callback / webhook | Host webhook handler + HMAC | Authenticity gate before Verify |
| Query / status | StatusQuery HTTP GET | `?reference=` against `StatusQueryBaseUrl` |
| Reconcile | `IPaymentDirectory.ReconcileAsync` / stale worker | Re-runs Verify for Pending |
| Refund | `IPaymentRefundGateway.RefundAsync` | Production: fail-closed until configured |
| Error classification | `GatewayVerification.FailureCode` | Includes TIMEOUT / UNAVAILABLE / RATE_LIMITED / PENDING / REJECTED / MISCONFIGURED |

## Isolation

- Order domain does **not** embed provider HTTP or brand logic.
- Provider code is registry-resolved (`webhook`, `fake`, fail-closed).
- No commercial PSP SDK or brand constants in application code.

## Adequacy for this Task

Contract is complete for a production-ready **boundary**.  
A concrete bank adapter remains blocked by `NO_PRODUCTION_PROVIDER_TARGET`.
