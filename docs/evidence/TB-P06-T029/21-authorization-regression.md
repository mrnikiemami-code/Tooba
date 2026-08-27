# 21 — Authorization regression (TB-P06-T029)

Representative checks for commercial gate. Distinguish **this session** vs **inherited ACCEPTED**.

## This session — observations

| Probe | Result | Careful reading |
| --- | --- | --- |
| Wallet read for unknown / non-demo actor | **200** | Host may **create an empty wallet account** on first touch. Treat as **observation**, not proof of cross-tenant data leak. Does **not** demonstrate reading another customer’s funded ledger. |
| Seller ACL roles | **200** with seller context | Healthy |
| Admin ACL | **200** with admin actor `01a036c2-970e-7000-8eb7-94bf5cc2d8db` | Healthy |

## Inherited ACCEPTED isolation (not re-run as full matrix this session)

| Check | Source | Outcome |
| --- | --- | --- |
| Seller isolation via `SellerPartyId` / seller headers | Prior ACCEPTED commercial + ACC tasks (orders/fulfillment/returns/settings) | Foreign seller data denied / scoped |
| Seller settings foreign deny | T027 | **403** |
| Employee without `seller.settings.*` | T027 | **403** |
| Category-scoped employee Mobile allow / Books deny | T024-R1 Host.Tests | PASS |
| Support ownership isolation | T025 ACCEPT + this demo ticket thread under owning customer | Proven path |
| Wallet spend stranger deny (suite) | T028 tests evidence | Documented in T028 |

## Customer foreign Order/Ticket/Wallet

| Aspect | Note |
| --- | --- |
| Order/ticket ownership | Commercial demo used owning customer `aaaaaaaa-aaaa-4aaa-8aaa-000000000009` only |
| Wallet unknown-actor **200** | Empty-account creation risk — **document only**; do not claim unauthorized funded-balance access |

## Tenant

Module schemas / CommerceContext isolation unchanged from prior ACCEPTED owners.

## Verdict

No new cross-seller leak found. Wallet unknown-actor **200** recorded carefully as empty-account behavior. Scoped employee + seller isolation remain grounded in ACCEPTED T024/T027 evidence.
