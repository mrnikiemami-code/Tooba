# 01 — Runtime before acceptance repair (TB-P06-T016-R1)

## Task framing

| Field | Value |
|---|---|
| Task | TB-P06-T016-R1 Locale-Prefixed Routing Acceptance Repair |
| Parent | TB-P06-T016 (implementation shipped; Architect = `REPAIR_REQUIRED` for evidence/acceptance contract) |
| Predecessor commit | `5db5ddc220842a98e0447bafa4d885edf62397a6` (`feat add locale-prefixed public routing [TB-P06-T016]`) |
| Branch | `main` |
| Pipeline | BRIDGE-WAKE-V1 / `tooba-main` |
| Bridge UUID | `af52fa7d-9b02-4664-9b6f-bad81f44879e` |
| Recorded proof | `docs/evidence/TB-P06-T016-R1/_acceptance-proof.json` @ `2026-08-27T03:18:16.224Z` |

## Architect decision (why this Repair exists)

TB-P06-T016 Worker PASS was **too terse** to satisfy the acceptance contract. Implementation of locale-prefixed public routing already landed on predecessor `5db5ddc`. This R1 Task must:

1. validate that implementation on a live Host + Frontend + Shopeiva triad;
2. produce audit-ready runtime / browser / SEO evidence (this folder);
3. repair only defects discovered by validation;
4. update Source-of-Truth correctly.

Out of scope: new multilingual features, UI redesign, Story kickoff.

## Baseline health (pre-evidence / live triad)

| Probe | Result |
|---|---|
| `GET http://127.0.0.1:5088/health/live` | **200** |
| `GET http://127.0.0.1:5088/health/ready` | **200** |
| Frontend origin `http://127.0.0.1:3000` | Up (locale routes served) |
| Shopeiva `http://127.0.0.1:3001/` | **200** |

## What was already on main (T016 ship)

- Public routes under `/{locale}/...` with middleware rewrite + `x-tooba-locale`
- Unprefixed public paths **308** → default locale (`fa`) with query preserved
- `LocalizedLink` / `localePath` / canonical + hreflang helpers
- Localized sitemap emission
- Page Composition called with locale-aware content API codes

## Gaps discovered during R1 validation (repaired)

| Defect | Repair |
|---|---|
| Client `documentElement.lang` / `dir` could stay stale after client navigation / hydration relative to public URL prefix | `LocaleProvider` now syncs `document.documentElement.lang` / `dir` from the public URL prefix (`parseLocalePrefix`) |
| Stale `.next` cache produced zod vendor-chunk **500** on some routes | Cleared `.next` cache and restarted Frontend; routes returned 200 after restart |

## Fixture slugs used for live proof

| Kind | Slug | Notes |
|---|---|---|
| Product | `demo-game-3` | Both `/fa/products/...` and `/en/products/...` = 200 with mutual hreflang |
| Article | `guide-online-shopping` | **fa-IR content only**; EN article route falls back honestly; **no fake hreflang** on article |

## Evidence package plan

| File | Purpose |
|---|---|
| `02`–`09` | Route / redirect / link / RTL-LTR / SEO / sitemap / composition proofs |
| `10` | No visual regression (routing/SEO only + captures) |
| `11`–`12` | Validation + final runtime |
| `captures/01`–`10` | Desktop/mobile PNG captures |
| `_acceptance-proof.json` | Machine-readable probe dump |
