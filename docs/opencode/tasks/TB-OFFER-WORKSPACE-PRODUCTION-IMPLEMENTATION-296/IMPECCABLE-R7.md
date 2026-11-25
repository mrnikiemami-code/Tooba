# Impeccable audit — TB-296-R7 visual & UX parity

Mode: Operate. Authority: approved OpenDesign 295 artifact (`index.html` SHA-256 `DA8AAFBF…F01A2`, `tab-1..7.png`, `mobile-360.png`).
Evidence: `evidence/r7-final-shots/` (all seven tabs at 1440 and 360), `evidence/r7-runtime/`, `evidence/r7-shots/orders-1440.png`, `orders-360.png`.

## Before (independent audit, R6 output)
- Verdict **PASS_WITH_FINDINGS**, score ~76/100.
- P1: placeholder/blue tiles, raw variant/SKU codes in header/product summary, one invalid seller-edit screenshot.
- P2: no offer code/Jalali save stamp, sparse density, raw amount input vs formatted display, clipped seller names, mobile tab clipping, single pencil action not matching Orders.
- P3: redundant shipping success, create-mode price caption, action order.

## Changes made from the critique
- Header now mirrors the artifact: 4-part summary grid, 46px thumbnail, brand + unit (no raw variant code), real Jalali `ذخیرهشده` timestamp, status/buyability, price + market/currency, single primary save + cancel cluster.
- Tabs: numbered ۱..۷, 44px, active green underline, dirty dot, disabled lock, active tab kept in view.
- Cards: identity split with readiness aside; pricing table + amount panel; inventory 3 tiles + per-warehouse table; shipping responsibility/type; conditions three cards; honest gated discounts/history.
- List: product first in RTL after selection with real media (cover) and brand; seller name (admin, including a mobile line); Jalali dates with Persian digits; understandable currency; compact Orders-style Eye action pinned left; `rowHeight=64`; headers widened/shortened to avoid truncation.
- Language: removed internal vocabulary; raw UUID gone from the main view; variant code lives only in a labeled `کد تنوع` field; shipping service control removed; saved state confirmed by a real detail re-read.
- Mobile: 44px controls, horizontal tab scroll with active-tab alignment, stacked inventory cards, sticky bottom action bar, zero page overflow.
- Geometry: tabs 44 vs 44–46, controls 37/44 vs 37/44, card radius 9 vs 9; header within 26px of the artifact’s sticky header at the tested widths.

## After (independent re-audit)
- Verdict **PASS_WITH_FINDINGS**, score ~85/100.
- Prior P1/P2 items resolved. Residual at that audit: row subtitle clipping, header truncation, technical variant code in the readiness card, single-action vs Orders kebab, tablet column visibility.
- Post-audit fixes: `rowHeight={64}`, header width/label tuning, wrapped labeled variant code, readiness now says `انتخابشده`, mobile seller line. Remaining: Orders’ extra kebab menu is not duplicated because the offer list has no second row action; tablet/narrow widths use the shared horizontal grid scroll.
- Mechanical detector: `[]` (`evidence/r7-impeccable-detect.json`). Empty detector output is **not** treated as proof; the screenshot review and geometry measurements are the evidence.

## Residual / data-limited (not fabricated)
- Dev seed media is a generic placeholder asset; the UI renders real media when present, with no invented product photo.
- Dev seed seller names/SKUs are real data; no business identifiers were invented.
- Per-offer discounts, per-offer shipping service assignment, offer history, multi-currency price rows remain gated.
- The editable price input stays raw while the table and preview show Persian grouped amounts; formatting the live input caused a real concatenation risk, so it was rejected.

## Verdict
Worker visual parity is **PASS_WITH_FINDINGS** against the approved artifact, conditional on owner acceptance. Worker PASS ≠ Architect ACCEPT ≠ owner visual acceptance.
