# 20 — P05 deferred items (TB-P05-T026)

Honest classification of remaining genuine items. Do **not** silently implement later-phase scope inside this gate.

| Item | Classification | Notes |
|---|---|---|
| Wallet (customer) | **Later Product Phase** | Honest unavailable UI; no ledger module |
| Support tickets | **Later Product Phase** | Honest unavailable; no ticket module |
| Gift cards | **Later Product Phase** | Honest unavailable; no gift-card module |
| Seller / admin analytics dashboards (deep BI) | **Later Product Phase / Hardening** | Live KPIs only where Host exposes; no fake charts |
| Settlement / payouts | **Later Product Phase** | No fake revenue settlement UI |
| Q&A admin moderation console | **Later / P06-adjacent** | No dedicated Admin Q&A product UI in P05 |
| B2B / wholesale / contract pricing UX | **Post-sale / B2B** | Authorization may allow request; commercial B2B UX deferred |
| Production SpiceDB hosting + session-bound seller users | **Hardening / Environment** | Dev InMemory + fail-closed Disabled until configured |
| Full server-side saved views / enterprise export | **Hardening** | Data Grid foundation + honest export notice only |
| Notifications / shipment tracking (unsupported paths) | **Later Product Phase** | Remain honest unavailable |

## Bucket summary

| Bucket | Items |
|---|---|
| **P06** | Items that naturally belong to next operational/hardening phase once Architect opens P06 (e.g. deeper ops polish adjacent to Q&A admin if issued) |
| **Later Product Phase** | Wallet, tickets, gift cards, settlement, deep analytics, unsupported notification/tracking |
| **Hardening** | Production SpiceDB topology, enterprise grid persistence/export |
| **Post-sale / B2B** | Wholesale / contract commercial UX |
| **Environment-only** | Hosting, secrets, SpiceDB deploy — not product invent |

None of the above are silent P05 blockers when honesty + live sellability path are intact.

**Deferred audit: COMPLETE (classified)**
