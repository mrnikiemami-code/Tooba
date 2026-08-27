# 15 — Customer payment truthfulness

**Task:** TB-P06-T022  
**UI lock:** Checkout + Payment Result remain Shopeiva-derived; no redesign.

## Truth table

| Backend truth | Customer-facing state |
|---|---|
| Pending / indeterminate | Pending (not Success) |
| Verified Succeeded | Success |
| Verified Failed (definitive) | Failed |
| Browser return URL alone | UX only — insufficient for Success |

## Retry

Retry path must re-enter safe initiation/verify/reconcile flows without inventing Success from client parameters.

## Development sandbox

`POST /v1/storefront/payments/{id}/sandbox/complete` remains Development/Testing only and must not be available as a Production success cheat.

## Visual

Any FE touch limited to truthful data binding.  
Unauthorized visual change ⇒ VISUAL REGRESSION (Task cannot PASS).

```text
CUSTOMER_PAYMENT_TRUTHFULNESS = LIVE (backend rules)
USER_VISUAL_ACCEPTED = NOT CLAIMED
```
