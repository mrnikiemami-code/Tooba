# TB-SHIPPING-MVP-OPTION-PARITY-069 — summary

Status: **PASS** for the bounded mission (A–C, E); **PARTIAL** for D (destination resolver diagnosis) and
for the admin-dialog field. Host restored with the shadow gate OFF.

| item | result |
| --- | --- |
| Option-aware policy identity | implemented: `(serviceCode, optionCode?, scope)` |
| Deterministic precedence | option tier first, then deepest scope; equal specificity = `ScopeConflict` |
| Cross-option leakage | none (engine tests + live numbers for standard vs express) |
| Additive migration | `20261015030159_OptionLevelRatePolicy` applied to `tooba_alpha` only |
| Existing rows | untouched, `option_code = NULL`, behave exactly as before |
| Admin authoring via API | proven live: two option rows created, generic row superseded |
| Invalid option guard | `400 shipping.service_option.unknown` (Fulfillment Contracts catalog seam) |
| Per-method runtime parity | 5/5 `Match` with explicit numbers (see below) |
| Legacy projection invariance | payloads identical OFF vs ON |
| Focused tests | Shipping 109/109, Order 286/286 (shadow subset 31/31) |
| Host restore | healthy on :5088, `ShadowEnabled`/`PrerequisitesConfirmed` removed, 0 shadow log lines |
| Destination resolver (D) | **not diagnosed** — named blocker for 070 |
| Admin rates dialog option field | **not added** — API is sufficient, UI follow-up |

Live per-method evidence (Development `tooba_alpha`, alborz area scope, validated geography ids):

```
service=post         option=standard legacy=120000 shadow=120000.00 outcome=Match
service=tipax        option=standard legacy=180000 shadow=180000.00 outcome=Match
service=tipax        option=express  legacy=260000 shadow=260000.00 outcome=Match
service=store_courier option=none    legacy=75000  shadow=75000.00  outcome=Match
service=in_person    option=none     legacy=0      shadow=0.00      outcome=Match
```

The 068 `AmountMismatch` for `tipax:express` is resolved by identity, not by widening the fallback.

Cost: two Host restarts (stop-before-build honoured), one module-scoped migration, four admin HTTP writes,
no order/payment/customer-facing surface touched, no Checkout change.
