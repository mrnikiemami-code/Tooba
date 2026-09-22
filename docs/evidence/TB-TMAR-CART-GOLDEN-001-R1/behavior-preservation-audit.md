# behavior-preservation-audit

Intended change: INTERNAL failure classification quality only.

| Scenario | Preserved outcome |
|---|---|
| guest secret invalid | Result → cart.guest.invalid (401) |
| access denied | Result → cart.access.denied (401) — distinct catalog code (was collapsed into guest.invalid by Contains heuristic) |
| missing cart | Result → cart.missing (404) |
| current authenticated cart missing/auth | Result → cart.missing / checkout.authentication_required |
| merge auth requirement | Result → checkout.authentication_required (401) |
| stale expected version | Result → cart.version.conflict (409) |
| expired cart code | Result → cart.expired (409) when exact code emitted |
| invalid quantity | Result → cart.quantity.invalid (400) |
| missing line | Result → cart.line.missing (404) |
| inactive/unavailable offer | Result → cart.offer.unavailable (400) |
| inventory insufficient/missing | Result → cart.inventory.insufficient (409) |
| inventory stale | Result → cart.inventory.stale (409) when exact code emitted |
| converted/non-active mutation | Result → cart.rejected (400) via cart.line.requires_active |
| unexpected IOE (e.g. cart.pricing.quote_missing) | propagates to global exception handling (NEW correct behavior) |

Checkout PAUSED_AT_SAFE_W5_CHECKPOINT preserved. Tax/Pricing/frontend untouched.
