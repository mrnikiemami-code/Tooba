# Offer Shipping Method Binding — Preview Verification

Status: **PASS — STOP_FOR_USER_DESIGN_APPROVAL.** Design verification only; no development, redesign, backend, or migration.

- HEAD `172ce2a10909cde6136ad3c4da71e568b0ed1207`; Next rebuilt and served on existing 3000, BUILD_ID `DPVucS4nqh6ZqFce4emB8`.
- Preview: `http://127.0.0.1:3000/fa/design/offer-shipping` (`/en/`, `/ru/`); real Playwright at 1440×900 and 390×844; no screenshots.

| Case | Result |
|---|---|
| Dropdown visibility | PASS — popup visible, non-zero, not clipped (Platform/Heavy: 5 eligible methods) |
| Internal search | PASS — name (“کامیون” → truck) and code (`FREIGHT` → freight) filter inside the popup |
| Add / remove multiple | PASS — add truck + light-truck (2 chips), remove truck (1), re-add truck (2) |
| Owner filtering | PASS — Seller/Heavy → seller-truck only; Platform/Heavy → platform methods, seller method excluded |
| Type filtering | PASS — normal→post, plus→plus-delivery, extra-heavy→extra-truck, grocery→grocery-delivery |
| RTL / LTR | PASS — fa dir=rtl and registry fa locale; en dir=ltr |
| Mobile 390×844 | PASS — no horizontal overflow; popup within viewport; method selection adds a chip |

Evidence: `shipping-design-evidence/playwright-verification.json`; verification script `shipping-design-verify.cjs`. No product/operational or backend changes.
