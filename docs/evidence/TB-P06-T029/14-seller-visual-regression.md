# 14 — Seller visual regression (TB-P06-T029)

Compare Shopeiva Vendor routes. **No redesign.** Access Control must remain native (not generic enterprise ACL).

## Surfaces audited (runtime open / inventory)

| Route | URL |
| --- | --- |
| Shell / dashboard | http://localhost:3000/vendor-panel |
| Products | `/vendor-panel/products` |
| Orders | `/vendor-panel/orders?sellerPartyId=01a030d1-40cb-7000-8abe-6d31739956c5` |
| Returns | `/vendor-panel/returns/...` (seeded return in demo) |
| Reviews / stories / notifications / tickets | LIVE nav |
| Access control | `/vendor-panel/access-control?sellerPartyId=...` · **200** |
| Settings | `/vendor-panel/settings` · **200** |

## Findings

| Item | Result |
| --- | --- |
| ACL native-fit | Continues shared Access Control UI from T024 (Shopeiva-mapped), not a foreign CRUD ACL skin |
| Unauthorized deviation | None newly requiring repair this gate |
| Analytics charts | Honestly deferred (see `02`) |

## Verdict

Seller visual contract remains Shopeiva-derived; ACL + settings surfaces open and native-fit retained from ACCEPTED prior tasks.
