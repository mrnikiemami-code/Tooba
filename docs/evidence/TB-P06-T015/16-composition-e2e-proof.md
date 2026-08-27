# 16 — Composition E2E proof (TB-P06-T015)

Artifact: `_composition-e2e-api-proof.json`  
Recorded: `2026-08-27T02:29:35.436Z`  
Admin actor: `01a036c2-970e-7000-8eb7-94bf5cc2d8db`

## Results

| Step | Status / observation |
|---|---|
| Runtime health + surfaces | all 200 (`hostLive`, `home`, `adminComposition`, panels, Shopeiva) |
| Baseline section types | 11 types present |
| Reorder | 200 — first two become `stories`, `hero` |
| Hide `brands` | 200 — brands absent from public list; `brandsVisibleAfterHide: false` |
| Restore default | 200 — full default order restored |
| Forbidden config | rejected (`rejectedForbiddenConfigStatus: 404`) |

## Captures tied to proof

- `04-tooba-home-reordered-desktop.png` — stories before hero
- `05-tooba-home-brands-hidden-desktop.png` — brands hidden
- `01` / `07` — default restored desktop/mobile
