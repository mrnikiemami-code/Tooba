# 02 — Visual governance lock proof (TB-P05-GATE)

## Surfaces covered

| Surface | Governance anchor |
|---|---|
| Storefront / Home / PDP | `AGENTS.md` rule 17; `docs/visual-baselines/CRITICAL-STOREFRONT-VISUAL-REVIEW.md`; HOME/PDP fidelity contracts |
| Listing / Search | `listing-structure.guard.test.ts`; TB-P05-T020 evidence |
| Cart / Checkout | Shopeiva shells + live commerce (T021) |
| Customer / Seller / Admin | Shopeiva-compatible panels (T022–T024) |
| Future UI | Same source-first workflow locked below |

## Required workflow (locked)

```text
actual Shopeiva source
→ component / JSX
→ CSS / Tailwind classes
→ interaction / hover / focus / active
→ animation / carousel / autoplay
→ responsive / mobile behavior
→ reuse/port implementation
→ replace demo bindings with live Tooba data only
```

## Forbidden (locked)

- screenshot-only approximation
- reinterpretation / taste-based simplification
- unauthorized modernization
- generic card/header/tab replacement
- **Unauthorized visual deviation = VISUAL REGRESSION**

## Fidelity dimensions (locked)

Structure + CSS + spacing + typography + shadow + hover + transition + motion + carousel/autoplay + overlay + badges + icons + micro-interactions + responsive/mobile + density + rhythm.

## Proof artifacts

- Automated: `npm run test:critical-storefront`
- Contracts: `docs/visual-baselines/HOME-FIDELITY-CONTRACT.md`, `PDP-FIDELITY-CONTRACT.md`
- Latest repair evidence: `docs/evidence/TB-P05-T026-R2/12-home-css-motion-parity-map.md`

**Visual governance lock: CONFIRMED**
