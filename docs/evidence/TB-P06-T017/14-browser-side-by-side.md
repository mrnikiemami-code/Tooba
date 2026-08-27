# 14 — Browser side-by-side (TB-P06-T017)

Proof script: `scripts/prove-t06-t017-stories.mjs`  
Outputs: `_acceptance-proof.json` + `captures/`

## Captures

| # | File | Subject |
|---|---|---|
| 01 | `captures/01-tooba-home-stories-rail.png` | Tooba `/fa` home stories rail (live circles) |
| 02 | `captures/02-tooba-story-modal.png` | Tooba story modal after circle click |
| 03 | `captures/03-shopeiva-home-stories.png` | Shopeiva `http://127.0.0.1:3001/` reference rail |
| 04 | `captures/04-admin-stories.png` | Tooba `/admin/stories` |

## Expected browser checks

- `[data-testid="home-stories"]` hydrates
- ≥2 story circle buttons
- Modal/fullscreen overlay after click (progress / `z-[200]`)
- Shopeiva home still shows «استوری» reference chrome
- Admin page reachable with stories UI

Captures are produced when Host :5088, FE :3000, and Shopeiva :3001 are up and the proof script is run. If PNGs are not yet present in this folder, regenerate via the proof script before Architect review.
