# 19 — SoT update notes (TB-P06-T017)

Worker/Architect should update durable SoT after ACCEPT (Worker does not invent ACCEPT). Suggested notes for:

## `docs/PROJECT-STATE.md`

- Current task line: TB-P06-T017 Stories End-to-End — Worker PASS awaiting Architect (or ACCEPTED once Architect says so).
- Capability: **Story module live** — schema `story`, public `/v1/storefront/stories`, admin `/v1/admin/stories*`, seed StoreAlpha, FE live rail+modal, admin `/admin/stories`.
- Readiness flag: `STORY_LIVE_WITH_EXACT_SHOPEIVA_UI = true` (or equivalent prose).
- Explicitly **not**: `PRODUCT_FULLY_READY`.
- Evidence path: `docs/evidence/TB-P06-T017/`.
- Predecessor commit: `9b4ff5c26981aa876565a6812da91bebafec169d`.

## `docs/ROADMAP.md`

- P06 row / IN_PROGRESS blurb: note T017 stories live + exact Shopeiva UI binding after Architect ACCEPT.
- Keep visual contract Shopeiva-locked; stories now Host-backed.

## Do not claim

- Full product commercial readiness
- Customer-created stories / AddStory
- Redesign of Home/PDP beyond stories live bind
