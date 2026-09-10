# TB-P09-T022-R2 — guest UI boundary

## Product boundary

There is **no separate** guest-only tracking route. Guest order viewing uses the **same** Customer Panel order page:

```text
/fa/customer-panel/orders/{checkoutId}
```

Guest proof is supplied via storefront cart session:

- `sessionStorage` key `tooba.storefront.guestSecret` (see `storefront-cart-api.ts` / `readCartSession`)
- FE fulfillment client forwards `X-Tooba-Guest-Secret` to BFF → Host

## What this repair does / does not invent

| Item | Status |
| --- | --- |
| Invent new guest tracking UI | **No** |
| Document existing shared page + sessionStorage secret | **Yes** |
| Host/BFF guest fulfillments proof | **T021-R1 / T022-R1 remains sufficient** |

Guest-alone Host call without secret → **404** (see `r2-auth-negative.md`). Wrong secret remains denied per existing Host convention.

The missing R1 proof closed here is the **authenticated owned** Customer UI path, not a new guest product surface.
