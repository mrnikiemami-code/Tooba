# RESULT — TB-SELLER-OFFER-SAFE-EDIT-AND-INVENTORY-INTEGRITY-283

Status: **PARTIAL** — implementation landed; connected real-backend browser proof and PostgreSQL concurrency proof remain open.
Readiness: **NOT_READY**. Worker self-review only; not Architect ACCEPT or owner acceptance.

## Implemented

- Offer detail now exposes named inventory positions; the aggregate is read-only.
- Stock writes accept an explicit `StockItemId`; the server binds it to the same offer/seller.
- Multi-position writes with no target are refused (`inventory.position.id_required`); foreign targets and below-reserve values are refused with zero change.
- The seller form saves only changed sections: commercial patch, price, and each named position independently.
- Draft is accepted as a no-op state for SKU edits; both limits can be cleared with `clearOrderQuantityLimits`.
- Client rejects wrong/stale response identity or effect, preserves other section drafts, and uses an independent GET to reconcile an uncertain response without a second write.
- The existing VendorShell page has explicit aggregate-vs-position copy, dirty state, save/cancel ids, and 360px-safe positions/controls.

## Acceptance status

| REQ | Browser | PostgreSQL | Result |
|---|---|---|---|
| REQ-01 | `REQ-01 SKU-only save keeps positions 3/7 and sends no price or stock write` — PASS | Split Offer/Inventory rollback tests; no single connected Host+PG test | **PARTIAL** |
| REQ-02 | `REQ-02 Draft SKU edit and explicit both-limit clear` — PASS | `Draft_sku_and_cleared_limits_persist_without_status_transition` — PASS | **VERIFIED** |
| REQ-03 | `REQ-03 a stale SKU-only form does not overwrite a newer external price` — PASS | NOT_RUN (real Pricing + concurrent PG harness absent) | **PARTIAL** |
| REQ-04 | `REQ-04 named second position 7→8 keeps 3 and total 11` — PASS | `Named_position_write_changes_only_that_position` 1/1 | **VERIFIED** |
| REQ-05 | `REQ-05 wrong response and lost response never claim save` — PASS | controlled HTTP | **VERIFIED** (browser) |
| REQ-06 | `REQ-06 cancel sends no writes and mobile dirty form is reachable` — PASS at 360×640 | controlled HTTP | **VERIFIED** (browser) |
| REQ-07 | `REQ-07 no-change save sends zero writes and a failed first section keeps the other draft` — PASS | controlled HTTP | **VERIFIED** (browser) |

## Verification

- Playwright/Chromium: **7/7** in `browser/assertions.json`; run log `browser/run-final.log`.
- PostgreSQL rollback-only: **2/2** in `postgresql.log` (`SellerOfferSafeEdit283RelationalTests`, `OfferSkuPatch283RelationalTests`).
- Offer focused: `Draft_sku_patch_stays_draft_and_clears_both_limits` **1/1**.
- `npx tsc --noEmit`: 0.
- `npm run build`: success, `BUILD_ID BBwBaSOakfco4OUr4yzrP`.
- `dotnet build src/backend/Tooba.slnx -c Release`: 0 errors.
- Host `5088` PID `34164`, DLL SHA256 `E301B13E558899F59F19505EB7059D3BFC65518BD1DAD035D223F0DDAB321019`, `/health/ready` 200.

## Open items

- REQ-01 lacks one connected browser → real owning Host → independent GET/PostgreSQL proof. Browser is controlled HTTP; PostgreSQL is split across Offer and Inventory rollback tests.
- REQ-03 has no real Pricing/PostgreSQL concurrency evidence. Browser controlled HTTP passes.
- Focused module runs still report unrelated guard failures: Offer `OfferArchitectureGuardTests` reads an absent ProductWorkspace handler; Inventory reports `Replacement` folder and missing catalog/localization for `HeldReleaseIntentConf`. They are recorded, not hidden.
- No screenshots; browser geometry is DOM/box evidence only.

## Safety

No migration, DDL, seed, grant, reset, live offer/price/stock/reservation write, provider, payment or notification effect. No user data mutation.
