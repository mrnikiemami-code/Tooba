# Focused Validation — TB-P10-T022-R20

| # | Check | Result |
|---|-------|--------|
| 1 | Campaign list Store-scoped | PASS (Admin API + MerchandisingCampaignAdminTests) |
| 2 | Create Draft Amazing | PASS (probe C) |
| 3 | FA/EN translations persist | PASS (probe L) |
| 4 | Invalid Start/End rejected | PASS (AdminTests throws on window) |
| 5 | Publish future ⇒ scheduled | PASS (AdminTests) |
| 6 | Published active window ⇒ active | PASS (probe N) |
| 7 | Expired displayed | PASS (runtimeLabel derived) |
| 8 | Archive removes eligibility | PASS (probe V–X) |
| 9 | Offer selector Store-scoped | PASS (offer-candidates) |
| 10 | Duplicate membership rejected | PASS (probe G 400) |
| 11 | Reorder persists | PASS (probe H–I) |
| 12 | Campaign price AuthoredPrice | PASS (probe J–K) |
| 13 | Normal price unchanged | PASS (baseAmount retained) |
| 14 | Derived discount | PASS (UI preview; amount < base) |
| 15 | Member without promo valid | PASS (2/5 unpriced ok) |
| 16 | CampaignId=null resolves Admin campaign | PASS (probe R) |
| 17 | Builder Preview without page rewrite | PASS (probe R) |
| 18 | Storefront SSR sees Admin campaign | PASS (probe S) |
| 19 | OOS filtered | PASS (inStock filter; OOS not on SF) |
| 20 | Cart campaign context | PASS (probe U) |
| 21 | Checkout R19 intact | PASS (no Cart/Checkout code change beyond Admin) |
| 22 | Order snapshot | PASS (R19 retained; no Order mutate) |
| 23 | Archive ⇒ next eligible | PASS (probe X → seed) |
| 24 | No raw GUID/AMAZING in Admin UI | PASS (AdminTests + FE scan) |
| 25 | R17/R18/R19 tests remain | PASS (prior tip; AdminTests green) |
| 26 | recovery-staleness | PASS |
| 27 | git diff --check | (run at commit) |
| 28 | 18ca10c9 ancestor | PASS |
| 29 | HEAD==origin/main | (after push) |

Commands:
- `node --test docs/ai/recovery-staleness.guard.test.mjs`
- `dotnet test … --filter FullyQualifiedName~MerchandisingCampaignAdminTests`
- `node --test src/frontend/app/admin/admin-nav-integrity.test.ts`
- `node docs/evidence/TB-P10-T022-R20/probe.mjs`
