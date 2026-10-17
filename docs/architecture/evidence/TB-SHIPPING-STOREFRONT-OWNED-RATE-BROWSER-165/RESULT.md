# TB-SHIPPING-STOREFRONT-OWNED-RATE-BROWSER-165 — RESULT

Status: **PARTIAL**

Base: `85083b40e1823add0e60597d185b0463cf5051a3` (`HEAD == origin/main` at claim time).

## What was delivered

1. Storefront consumes the additive `UnavailableMethods` response through the existing shipping
   API boundary (`mapShippingProjection` → `unavailableMethods: StorefrontShippingUnavailableMethod[] | null`).
   Missing/`null` property ⇒ `null` and the previous customer behavior is byte-for-byte preserved
   (OFF parity at the model level).
2. `storefront-shipping-unavailable-methods.ts` — translation of the stable server reason codes into
   short Persian text, with a truthful generic fallback for unknown/empty codes. Rules enforced in
   code: an unavailable row is explanatory only, never removes or overrides an eligible server
   method, never produces a price/`رایگان`/date, and is de-duplicated per method (owner-option
   pricing has no seller dimension, so a row is never attributed to a sibling seller/method).
3. `storefront-shipping-unavailable-methods.tsx` — read-only, `aria-disabled`, RTL block
   (`data-shipping-unavailable="true"`, per-method `data-testid`/`data-reason-code`, no button and no
   click handler), wired into both the single-group method card and the grouped-cart card surface.
4. `storefront-shipping.tsx` passes a memoized notice list; method selection still comes only from
   `projection.methods` (server-eligible), so an unavailable method cannot be selected and grouped
   carts stay fail-closed because a group is only built from an eligible method and only resolves
   with its own server horizon row.
5. Focused deterministic tests (`storefront-shipping-unavailable-methods.test.ts`, 35 assertions
   across 7 tests) + verification against the **real** TB-164 authoritative payload.

## Evidence (this directory)

| File | Content |
| --- | --- |
| `focused-tests-165.txt` | 36/36 pass (shipping API, unavailable model, group schedule, horizon, item groups) |
| `typecheck-165.txt` | `tsc --noEmit` clean |
| `mapping-on-real-payload-165.json` | Real TB-164 ON/negative projection through the shipped normalizer |
| `architecture-guard-165.txt` | `test:architecture` — 2 pre-existing failures, see below |
| `critical-storefront-guard-165.txt` | `test:critical-storefront` — 1 pre-existing failure, see below |
| `isolation-guard-165.json` | private scratch targets, untouched shared services, cleanup proof |

Real-payload proof (not a hand-written fixture): the TB-164 `projection-negative-restricted-164.json`
payload yields `tipax:standard` still eligible, `tipax:express` non-selectable with
`shipping.coverage.method_restricted` → «این روش برای مقصد انتخابی مجاز نیست.», and every notice is
method-scoped with no price. The TB-164 ON payload yields eligible `tipax:standard = 205000` and
`tipax:express = 480000` (frontend renders server values only).

## Guard failures (pre-existing, unrelated to this task)

Both guards fail on files that are **unmodified at the expected base SHA** (`git status --porcelain`
reports no change for them), i.e. the failures already exist at `85083b4`:

- `FE-SIZE` oversized growth: `app/admin/category-admin-screen.tsx`, `app/admin/landing-pages/admin-landing-page-composer.tsx`, `app/storefront/storefront-landing-blocks.tsx`, `lib/storefront-composition/shared-composition-renderer.tsx`.
- Surface-role violation `app/storefront/storefront-landing-blocks.tsx:286 bg-white`.

No such file was touched by this task; the new modules stay small (≈125/60/190 LOC) and no file over
800 LOC was introduced.

## Not done — exact blocker (why PARTIAL)

The required **real authenticated customer browser leg** was not executed:

- A disposable PostgreSQL container (`tooba-tb165-pg`, `:55485`) and a private Host publish
  (`.tmp-165-host-out`) were created, but a fresh scratch database has no catalog/seller/offer rows:
  TB-164's journey takes `TOOBA_164_SELLER_A/B`, `TOOBA_164_OFFER_A/B` from the environment and its
  committed harness contains no script that seeds the two sellers, their offers and the active tax
  rules into a new database. Rebuilding that seed set (catalog + offers + sellers + tax + Geography +
  rate policies) was not achievable inside the 45-minute timebox.
- Therefore there is **no** genuine browser screenshot, no DOM assertion from a live page, no live
  "two cards / 410000 payment preview / full-reload persistence" proof, and no live negative PUT case
  in this result. Nothing in this document may be read as a live browser certification.
- All scratch resources were removed; committed appsettings keep
  `Tooba:ShippingOwnedRateAuthority:Enabled = false` in `appsettings`, `appsettings.Development` and
  `appsettings.Production`.

## Next unblocking step

Provide (or authorize) a scratch seeding script that creates, in an empty private database: two
sellers, one purchasable offer per seller with stock/price, active tax rules for their categories,
the Geography chain, `shipping.rate_policies` for `tipax:standard`/`tipax:express`, and the rate
coverage declaration — then the browser leg (Playwright/Chromium against the private frontend with
`TOOBA_HOST_ORIGIN` pointing at the private Host) can run within the same timebox.

STOP_FOR_ARCHITECT.
