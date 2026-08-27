# 16 — Panel authorization proof (TB-P06-T018)

## Summary

Wave 1 is a **frontend presentation honesty** wave. **No authorization model change.**

| Concern | Wave 1 status |
|---|---|
| SpiceDB schema | Unchanged |
| Customer ownership | Unchanged |
| Seller Actor + SellerParty isolation | Unchanged |
| Admin authorization | Unchanged |
| Request-supplied identity authority | Still forbidden / unchanged |

## Expected behavior (existing, preserved)

| Actor case | Expected |
|---|---|
| Customer own profile/settings bridge | Allow (existing profile API) |
| Customer foreign profile | Deny (existing Host ownership) |
| Seller own operational settings read | Allow (existing seller dashboard API + ownership) |
| Seller foreign seller party | Deny (existing headers / SpiceDB) |
| Admin authorized operational routes | Allow |
| Admin unauthorized | Deny |
| Deferred deep-link shells | No new privileged APIs |

## Non-claims

- Wave 1 did not add new Host authorization endpoints for notifications/tickets/admin settings.
- Frontend nav hide is **not** an authorization control; Host remains the authority for mutations/reads.
