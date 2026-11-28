# Offer Shipping Method Binding — Design for Approval

Status: **DESIGN_APPROVAL_PENDING.** Design-only; no operational change, no migration, no Shipping-domain redesign, no other tab changed.

## Preview (real Next.js route)
- FA/RTL: `http://127.0.0.1:3000/fa/design/offer-shipping`
- EN/LTR: `http://127.0.0.1:3000/en/design/offer-shipping`
- RU/LTR: `http://127.0.0.1:3000/ru/design/offer-shipping`
- Source: `src/frontend/app/design/offer-shipping/page.tsx` + `shipping-preview.module.css` (design artifact only; the operational ShippingTab is untouched).

## Design intent
The existing Offer Shipping tab keeps its card/section rhythm and gains one owner-facing choice: how the offer binds to the market’s existing shipping methods. It does not create methods, edit services, or touch other tabs. The design is Operate-mode: scanability and precision over decoration, consistent with the shared Offer workspace.

## Field order (top → bottom)
1. **Fulfilment responsibility** — existing select: Unassigned / Seller / Platform. Changing it resets the dependent type; Unassigned disables the type and the method section.
2. **Shipping type** — existing enabled type select, filtered by responsibility. Changing type clears selected methods (they belong to the previous type).
3. **Method selection mode** — two segmented cards:
   - **Automatic**: every currently allowed method for the selected type/owner is applied; the picker is hidden and a short note shows the resolved scope.
   - **Specific**: only the methods the owner chooses are applied.
4. **Shipping methods (Specific only)** — selected methods as removable chips (localized name + Latin code, so duplicate names are distinguishable), plus an “Add shipping method” searchable combobox.

## Multi-select combobox
- Reuses the shared Design System `SearchableSelect` popup: internal search field, keyboard (Arrow/Enter/Escape), RTL-safe placement, full-width popup on mobile.
- Choosing a method appends a chip and closes the popup; removing a chip is a per-chip accessible action (`aria-label` includes localized name). Re-adding a removed method is possible.
- Only eligible methods are listed: active, valid for the selected shipping type, and owned/allowed for this seller. Inactive, invalid or another seller’s methods never appear.
- The popup lists localized name and code; chips keep the code visible so equal names remain distinguishable.
- No external search input and no “selected/unassigned” placeholder copy; the list is bounded and owner-filtered.

## RTL / locale / mobile
- All spacing uses logical start/end; the delete action is at the logical end of each row (left in RTL, right in LTR). Chips wrap naturally.
- Copy comes from the registry (fa/en/ru) and the direction follows the active locale; the preview demonstrates fa RTL and en/ru LTR. Numeric/code fields stay LTR.
- At ≤640px the two segmented cards stack, chips wrap, and the searchable popup is full-width. 390×844 is the reference mobile size.

## Reuse / accessibility
- Existing Offer card, field, control, chip and button tokens plus the shared `SearchableSelect`; no new design system, no new locale authority, no parallel shipping UI.
- Combobox: trigger is a labelled button with `aria-expanded`; popup has a `role=listbox`, search field is a combobox, options are `role=option`; chips have accessible remove labels; Escape closes and returns focus.

## Phase 2 (only after approval)
Owner contracts read existing shipping methods; persist `OfferShippingMethods` atomically with offer revision/CAS, history and facts; enforce selected-method scope in cart/checkout; group checkout allows only methods common to all offers; reject inactive/invalid/other-seller methods; preserve previous offers’ behavior. No migration in this design phase.

## Review questions for the architect
1. Automatic mode scope: all enabled methods for the offer’s shipping type and owner (proposed) vs all enabled methods in the market.
2. Should sellers be able to set Specific methods, or is the method binding admin-only while sellers see it read-only (proposed: mirror the existing responsibility/type admin-only behavior).
3. When an offer’s selected method later becomes inactive, show it as a retained-but-inactive chip with a warning (proposed) vs silently drop it.

## Quality notes
Impeccable detector on changed design files: 0 findings. No screenshots created. No production code path, storage, or migration was touched in this phase.
