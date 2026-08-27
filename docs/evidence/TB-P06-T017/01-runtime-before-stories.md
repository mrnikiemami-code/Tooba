# 01 — Runtime before stories (TB-P06-T017)

## Predecessor

| Field | Value |
|---|---|
| Task | TB-P06-T017 Stories End-to-End |
| Predecessor commit | `9b4ff5c26981aa876565a6812da91bebafec169d` |
| Branch | `main` |
| Pipeline | BRIDGE-WAKE-V1 / `tooba-main` |
| Bridge UUID | `43fff592-fb07-476a-9485-c8a526d79a13` |

## Baseline (pre-ship / pre-live Story module)

| Probe | Pre-T017 state |
|---|---|
| Host `http://127.0.0.1:5088/health` | 200 (operational Host) |
| `GET /v1/storefront/stories` | MISSING — no Story module / endpoints |
| Admin `/v1/admin/stories` | MISSING |
| Schema `story` + migration `InitialStory` | MISSING |
| Home stories rail | Fake category-circle `STORY_IMAGES` in storefront home |
| Shopeiva Story chrome on Tooba | Not live-bound (placeholder/fake circles) |
| Admin UI `/admin/stories` | MISSING / not live |
| PageComposition section `stories` | Present as section type; bound to fake/local UI only |
| Customer AddStory | N/A (Shopeiva demo only; not a Tooba product path) |

## Context carried in

- T015 Page Composition already lists section type `stories` and renders a home stories slot.
- T016 / T016-R1 locale-prefixed public routes (`/fa`, `/en`) + RTL/LTR foundation.
- Visual contract remains Shopeiva-locked; this Task connects **live data** without redesigning Home/PDP chrome.
