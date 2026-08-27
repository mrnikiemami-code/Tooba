# 02 — Authorization architecture audit

Task: TB-P06-T024

## Classification

| Area | Status | Notes |
|------|--------|-------|
| SpiceDB adapter / fail-closed | LIVE | Host `SpiceDbAuthorizationAdapter`, InMemory, FailClosed |
| Schema foundation user/tenant/party | LIVE | v2 → extending to v3 for ACC |
| AdminPanelAccess | PARTIAL | Binary `tenant#view` only |
| SellerPanelAccess | PARTIAL | Binary `party#view` only |
| PartyMembership → SpiceDB member | LIVE | Projection handler |
| Role / Permission entities | MISSING → building | AccessControl module |
| Permission catalog | MISSING → building | Semantic IDs, not endpoint names |
| User–role assignment | MISSING → building | |
| Delegation ceiling | MISSING → building | PlatformSellerCeiling ∩ grants |
| Resource scopes | MISSING → foundation | Typed ScopeKind; Category proven at policy layer |
| Fine-grained product workspace flags | HARDCODED | Header `X-Tooba-Workspace-Scope` — out of ACC rewrite scope |
| UI capability projection | MISSING → building | Nav/action from effective perms |

## Actor / party / tenant

- Identity UserId is SpiceDB `user` subject
- Admin: tenant membership
- Seller: party membership; SellerPartyId header is context only
- Customer: owner-key isolation (not ReBAC ACC)

## Hardcoded gaps addressed by T024

- No `if role ==` product authorization engine
- ACC uses catalog + dynamic roles + SpiceDB capability tuples
- Panel entry gates remain; fine-grained ACC permissions overlay via `accesscontrol.view|manage`

## Decision

Do **not** redesign SpiceDB from scratch. Extend schema + add AccessControl module as PG config SoT with SpiceDB enforcement sync.
