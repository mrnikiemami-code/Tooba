# 06 — Motion / interaction proof (TB-P06-T011-R2)

Artifact: `motion-proof.json` (CDP computed-style samples)

## Customer (Shopeiva modal)

- Open: click **مرجوع** → modal overlay appears (`02-shopeiva-customer-return-modal-desktop.png`)
- Interaction: `mouseover` on primary button — `transitionProperty` / `transitionDuration` recorded in `motion-proof.json` → `shopeiva-customer-modal`

## Seller (Shopeiva review modal)

- Open: `/vendor-panel/orders/1` → **بررسی درخواست** → modal (`04-shopeiva-seller-return-review-modal-desktop.png`)
- Two-step approve: first click sets `action=approved`, second executes (Shopeiva mock)

## Seller (Tooba grid)

- Surface: `/vendor-panel/returns` row link `a.font-semibold`
- Hover: `mouseover` event → computed color/background delta captured → `tooba-seller-grid` in `motion-proof.json`
- Buttons use `transition-colors` in `return-ui.tsx` and vendor grid links

## Admin (Tooba grid)

- Same DataGrid hover pattern as fulfillments (accepted T024 shell)
- Capture: `08-tooba-admin-returns-list-desktop.png` + grid hover class `hover:underline` on links

Static PNG alone insufficient — computed-state JSON included per protocol §F.
