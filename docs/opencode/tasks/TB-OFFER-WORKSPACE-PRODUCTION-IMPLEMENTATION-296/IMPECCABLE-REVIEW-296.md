# Impeccable review — TB-296 offer workspace

Mode: Operate (admin/seller task completion). Surface: shared Offer create/edit workspace.
Rendered evidence: `evidence/runtime/admin-tab-*.png`, `seller-edit.png`, `admin-mobile-360.png`.

## 1. Critique (pre/post implementation)
- **Hierarchy (strong).** Compact sticky header carries product, seller, offer id, status, buyability and price; the seven tabs remain the only navigation. Actions are a single primary "ذخیره تغییرات" plus انصراف.
- **RTL/LTR (strong).** Logical RTL layout throughout; UUID, SKU and numeric inputs are `dir="ltr"` islands; Persian digits/formatting used for money and counts.
- **Scanability (good).** Pricing is a single honest table row; inventory is a per-warehouse table with read-only reserved/available; conditions group returns, quantity and lifecycle.
- **Gated honesty (strong).** Discounts, history, multi-currency rows and Stage-3 service assignment are visibly explained as contract gaps; no fake events, discounts or prices.
- **Mobile (adequate).** Header wraps without overlaying content; tabs scroll horizontally; primary actions remain reachable; 44px minimum heights. Active tab is clear, but the far tab is partially off-screen at 360px (scroll affordance could be stronger).
- **Density (acceptable).** Edit identity leaves generous whitespace in one tab; acceptable for an Operate surface but could be tightened later.
- **Accessibility (good).** Native buttons/inputs/selects, labels around fields, `role="tab"`/`aria-selected`, dirty dots carry text in the header, status is not color-only (label text present), errors are textual.

## 2. Choices made from the critique
- Reused design-system `Button/Input/Select/Badge` and design tokens (`bg-surface`, `border-border`, `text-muted`, `rounded-ds`); no raw hex in the workspace.
- Kept the shared `OfferWorkspace` in `lib/offer-workspace` as role-agnostic presentation/composition; pages supply typed adapters. No parallel grid or form framework was created.
- Chose a truthful "needs contract" state for gated tabs instead of approximating Promotion/history behavior.
- Kept one primary action bar; price/inventory/returns are saved through the same bar with per-tab errors and no atomic-transaction promise. Fulfillment responsibility keeps its separate owning contract button.
- Used beforeunload + dirty indicators rather than blocking navigation dialogs.

## 3. Post-check
- Mechanical Impeccable detector over the eight workspace files: `[]` (no findings), see `evidence/impeccable-detect.json`.
- Real Chromium at 360/390/768/1280/1440: `scrollWidth - clientWidth = 0` at every size (`evidence/runtime/verify-runtime.json`).
- No fabricated data path: discounts/history have no records; pricing shows only the real resolved price; shipping has no service assignment.
- Remaining polish (not blocking): add a subtle start/end scroll fade to the mobile tab bar; tighten identity density; consider a read-only fulfillment read contract when one is authorized.

## 4. Score
- Visual hierarchy 8.5/10 · RTL/typography 8.5/10 · Operate scanability 8.5/10 · States/errors 8.5/10 · Mobile 8/10 · Honesty 9/10.
- Weighted ≈ **85/100**, no P0/P1 defect; not an independent owner/architect visual acceptance.
