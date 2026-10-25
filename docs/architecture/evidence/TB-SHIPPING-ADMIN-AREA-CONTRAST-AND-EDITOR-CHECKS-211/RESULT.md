# TB211 — area card contrast, one-shot editor checks, storefront status

Task: TB-SHIPPING-ADMIN-AREA-CONTRAST-AND-EDITOR-CHECKS-211, parent TB-SHIPPING-ADMIN-VISUAL-ALIGNMENT-210-R1,
base `a2e274d1275847305b6997d090380e0bd3295eae`, channel `tooba-main`, worker `tooba-worker-01`.

## A. Minimal contrast (alternating whole area card)

`ui/authoring-groups-editor.tsx` only. Each area `<section data-testid="shipping-authoring-group-draft">`
keeps its existing class set and now alternates the WHOLE card background by current display position:
even position keeps `bg-surface-elevated` (white), odd position gets an inline
`backgroundColor: rgb(var(--color-section-alternate))` — the existing theme-aware alternate-surface token
(`247 248 250` in light, `22 22 26` in dark). No new token, no global CSS, no design-system change, no
child opacity, no colour on cost rows, no model/persistence/save change.

Measured on the live page (EDIT, second area added locally and discarded, see §C):

| position | computed background | card x | card width |
| --- | --- | --- | --- |
| 0 (stored area) | `rgb(255, 255, 255)` | 56 | 1057 |
| 1 (second area) | `rgb(247, 248, 250)` | 56 | 1057 |

Same values at 390px wide; `document.scrollWidth == innerWidth == 390` → no horizontal overflow.
Because the token is redefined under `.dark`, the alternation stays readable in dark mode and inside the
palette-tinted storefront canvases (the token has a tint remap); the Admin canvas is unaffected.
Reorder parity comes from `position`, which is already the display index used by the header index label,
so up/down moves and add/remove keep strict alternation.

## B. Editor checks — enumerated, real paths, one run each

Files were enumerated RECURSIVELY under `features/admin-shipping-authoring` (not a root glob) and the
**14** actual paths were passed to the Node test runner:

```
features/admin-shipping-authoring/api/editor-document-adapter.test.ts
features/admin-shipping-authoring/authoring-general-ui.test.ts
features/admin-shipping-authoring/authoring-lifecycle-202-r1.test.ts
features/admin-shipping-authoring/authoring-rates-week-203.test.ts
features/admin-shipping-authoring/authoring-rates-week-203-r1.test.ts
features/admin-shipping-authoring/destination-picker-204-r1.test.ts
features/admin-shipping-authoring/destination-picker-204-r2.test.ts
features/admin-shipping-authoring/repair-200-r1.test.ts
features/admin-shipping-authoring/session/editor-session.test.ts
features/admin-shipping-authoring/session/editor-session-200-r2.test.ts
features/admin-shipping-authoring/shipping-groups-204.test.ts
features/admin-shipping-authoring/user-trial-209.test.ts
features/admin-shipping-authoring/user-trial-209-r1.test.ts
features/admin-shipping-authoring/user-trial-209-r2.test.ts
```

Result (`node --experimental-strip-types --test <the 14 paths>`):
`tests 119 · pass 119 · fail 0 · skipped 0` (119 includes the nested `api/` and `session/` files).

First run before any correction: `tests 119 · pass 118 · fail 1 · skipped 0`.
The single failure was `authoring-rates-week-203-r1.test.ts` R203-09 asserting `"Weight: 0–5,000"` while the
rendered, truthful axis label is `"Weight (g): 0–5,000"` (unit added by earlier accepted 209 work). The
production code was correct — a stale assertion, not a defect — so the **one** bounded correction was that
assertion following the real text (still asserts every axis is shown, incl. the open upper bound
`Merchandise: 100,000–—`). No assertion was disabled, loosened or deleted.

`npm run typecheck` → clean (`tsc --noEmit`, exit 0).
Existing guards, one run: `frontend-import-boundary`, `frontend-feature-boundary`, `frontend-flat-folder`,
`frontend-test-discovery` → `tests 7 · pass 7 · fail 0`. Not run: `frontend-source-size` / broader
architecture set (the four historical unrelated size failures were left untouched as instructed), no
backend/solution build, no discovery sweep.

## C. Real visual, zero business writes

FE3000 / Host5088 were already up; the user's tab was not reloaded (separate isolated tab).

- Actual current data: the draft `01a16eeb-70b3-7000-87c2-564c82835496` currently has **ONE** stored area,
  so a second LOCAL UNSAVED area was added in Edit purely to inspect contrast.
- Captured: `areas-alternating-1440x900.png` (both cards visible, computed colours above) and
  `areas-alternating-390x844.png` (two cards, paired widths, no overflow).
- Left Edit through the normal flow → the app's own «تغییرات اعمال‌نشده» confirm → «خروج بدون ذخیره».
  After the discard: back in view mode, areas = 1, no unsaved banner, Regions panel inputs = 0 and the
  stored freight/packaging still read `120000.00` / `5000.00`.
- No Apply, no POST/PUT/PATCH/DELETE, no seed, migration, new draft, reset, backend build or restart.

## D. Storefront question — source analysis (existing capability vs this editor)

| piece | state | evidence |
| --- | --- | --- |
| Draft editor persistence (type/areas/fees/week) | **implemented** | `Modules/Shipping/Tooba.Shipping.Application/Authoring/*` + `ShippingEndpointModule` authoring endpoints + authoring migrations; live save/readback accepted in 209 |
| Publish / activation / registry bridge from draft to runtime | **absent** | no publish/activate endpoint or port in the Shipping authoring surface; Order module has **zero** references to `Authoring` / `ShippingAuthoringDraft` (`grep` under `Modules/Order` = 0 matches) |
| Existing storefront shipping choice UI | **implemented (separate)** | `app/storefront/storefront-shipping.tsx`, `storefront-shipping-group-cards.tsx`, `storefront-shipping-delivery-card.tsx`, `storefront-checkout.tsx` |
| Its runtime source | **config + Fulfillment catalog, not authored draft** | the UI calls `/v1/storefront/shipping/{projection,selection,commit}` → `Order` `StorefrontShippingService`; method availability comes from `ShippingMethodRegistry.Enabled(ShippingMethodsOptions)` and amounts from `StorefrontShippingCalculator.ResolveRate(methodCode, ShippingMethodsOptions)` (configuration rates), labels from `IShippingCatalogReader` (Fulfillment shipping-services catalog) and `IShippingOneLevelCanonicalMethodReader` (gated one-level catalog, default OFF) |
| Do authored areas / fees / week reach customers today? | **no** | nothing in the storefront/checkout path reads the authored draft tables; the draft is authoring-only in its current slice |
| Draft type | **partially relevant, upstream only** | the type is a Fulfillment shipping-type id assigned to the draft; the storefront does not consume the assignment |

So: **the storefront side of shipping already exists** (customer delivery groups, method choice, cost, date
options) but it is **not yet integrated with this new draft editor** — there is no activation/publish seam, so
an authored draft cannot currently change what a customer sees. Feature tests passing does not change that.

Smallest next delivery slice (not implemented here): a read-only **publish/activation seam** that projects one
completed authoring draft into the storefront projection source that
`StorefrontShippingService.ProjectAsync` already reads — i.e. a drafted-method read port in
`Fulfillment.Contracts` (or a Shipping-owned read model consumed by Order), with an explicit
`Activated`/`Inactive` state and no change to `ShippingMethodsOptions` legacy defaults. Until that exists,
`default-OFF` and the current config-driven behaviour remain the authority.

## Validation and timing (honest)

Claimed 08:30:55Z. Contrast edit ~08:33, tests/typecheck/guards ~08:34, live visual ~08:35, source analysis
~08:38, docs/commit from ~08:40 — i.e. the implementation stop point (minute 12) was respected and the
finish phase stayed inside the 20-minute box. Elapsed reported from the real clock.

## Claim limits

Presentation + verification only. No save, quote, activation, readiness, deployment, certification or
production acceptance. **User Visual ACCEPT remains PENDING** — the worker does not grade its own UI. The
storefront section is a source reading, not a live storefront/checkout run. STOP_FOR_ARCHITECT.
