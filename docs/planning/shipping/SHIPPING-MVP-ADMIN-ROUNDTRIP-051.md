# TB-SHIPPING-MVP-ADMIN-ROUNDTRIP-051 — isolated roundtrip proof for `/admin/shipping-services`

- Task: `TB-SHIPPING-MVP-ADMIN-ROUNDTRIP-051` (Program `TOOBA-SHIPPING-MVP-SIX-TASK-CLOSURE`, milestone 2 of 6)
- Parent: `TB-SHIPPING-MVP-ADMIN-050`
- Mode: `BOUNDED_ADMIN_SHIPPING_SERVICE_MUTATION_ROUNDTRIP_VERIFICATION`
- Pipeline: `BRIDGE-WAKE-V1` / channel `tooba-main` / worker `tooba-worker-01`
- Expected-Base-SHA: `427bcad72d750561beac0322d7cae52081f9c8fe`
- Stop-State: `STOP_FOR_ARCHITECT`

## 1. Baseline

`HEAD == origin/main == 02aaf242c…`; Expected-Base is its grandparent. The two intervening commits are
the parent task's own evidence bookkeeping only (`docs/architecture/evidence/TB-SHIPPING-MVP-ADMIN-050/{RESULT.bridge.txt,post-result.js,results.json}`)
→ harmless fast-forward, no reset/stash/rebase/force-push. Pre-existing unrelated dirty files preserved.

## 2. What this closes

050 shipped a real fix but reported `PARTIAL`, because create / load-edit / save-update / option-edit /
confirm-deactivate were never exercised end to end (the only live attempt would have written Fulfillment
catalog rows and therefore changed real checkout shipping options). This task closes that gap with an
**isolated** run instead of a live mutation.

## 3. Approach

The screen and `shipping-services-api.ts` call global `fetch` and `window.confirm`; both boundaries were
intercepted **inside the already-running dev page** via CDP `Runtime.evaluate`, so the real React screen,
the real API client, the real DTO mapping and the real grid render against an in-memory Fulfillment
catalog. `/v1/admin/languages` and every other request passed through to the untouched backend, so the
fixture keys translations by the real active language ids. Injection script kept in
`docs/architecture/evidence/TB-SHIPPING-MVP-ADMIN-ROUNDTRIP-051/browser-fixture.js`; full transcript in
`browser-roundtrip.txt`.

A durable, reproducible in-repo regression was added alongside it:
`src/frontend/app/admin/shipping-services/shipping-services-roundtrip.test.ts` installs the same kind of
in-memory Fulfillment fixture on `globalThis.fetch` and asserts path, method, DTO shape, identity
preservation and every negative mapping.

No production file was changed: the 050 implementation already satisfied every assertion, so this task is
**verification-only**, with no scope-creep fix. (Repo cannot host a React render test: tests run on
`node --experimental-strip-types`, which cannot import `.tsx` JSX, and jsdom/Playwright/RTL are not part
of the existing setup — adding a framework would violate the "no invented framework/dependencies" rule.)

## 4. Verified flows (isolated, with exact evidence)

| # | Flow | Result |
| --- | --- | --- |
| 1 | Create: dialog → `POST /v1/admin/shipping-services/` | exact body captured; canonical `code`, per-language translations for all 3 real language ids, two options with `shippingServiceOptionId: null` for new rows; 201 → list refresh → `role=status` notice «سرویس «پیک دوچرخه» ایجاد شد.» → 3 rows |
| 2 | Load-edit: row action → `GET /v1/admin/shipping-services/{id}` | form hydrated with server values (code, name, option codes/names/active) |
| 3 | Save-update + option edit → `PUT /v1/admin/shipping-services/{id}` | one write; loaded option identity preserved (`dddddddd-…-001/002`); changed name/option code/option name/`isActive:false` all present; notice «… ذخیره شد.» |
| 4 | Validation gate | clearing نام نمایشی issued **zero** requests, kept the dialog open, showed the Persian inline error, left the fixture untouched (the 050 regression case) |
| 5 | Deactivate declined | confirm message captured; **zero** requests; service still active |
| 6 | Deactivate accepted | confirm message captured; `POST …/{id}/deactivate` + list refresh; notice «… غیرفعال شد.»; row flips to `خیر` and its deactivate action disappears |
| 7 | Load failure | `role=alert` notice «سرویس ارسال خوانده نشد؛ دوباره تلاش کنید.» visible while the dialog is closed |
| 8 | Write failure | `PUT` rejected → `role=alert` «این کد سرویس قبلاً ثبت شده است…»; no raw `shipping_service.` code leaked; dialog stays open; catalog unchanged |
| 9 | Deactivate failure | `POST …/deactivate` rejected → `role=alert` fallback «غیرفعال‌سازی ناموفق بود…»; service still active |

Complete request log for the run: 16 requests, each one explained by a user action — no phantom or
duplicate writes, and no write at all on validation failure or on a declined confirmation.

## 5. Safety / no live mutation

After the run the fixture was removed, the original `fetch`/`confirm` restored, and the page reloaded
against the real API. The Development catalog reads exactly as before:
`post ۱/۲ بله`, `tipax ۲/۲ بله`, `snapp_courier ۰/۰ خیر`, `store_courier ۰/۰ بله`, `in_person ۰/۰ بله`.
No database write, no Host restart/reconfiguration, no new route, no backend/contract/migration change,
no checkout or shared storefront change, and no fake Shipping rate/coverage control was introduced.

## 6. Validation run

| Check | Command | Result |
| --- | --- | --- |
| Typecheck | `cd src/frontend && npx tsc --noEmit` | PASS (0 errors) — `log-typecheck.txt` |
| Focused tests | `node --experimental-strip-types --test app/admin/shipping-services/shipping-services-roundtrip.test.ts app/admin/shipping-services/shipping-services-form-model.test.ts app/admin/admin-grid-migration.test.ts` | PASS 22/22 — `log-test-focused.txt` |
| Isolated browser roundtrip | see §4 | all 9 flows observed — `browser-roundtrip.txt` |

Broad suites were not re-run: this task adds no production code, and their pre-existing failures
(source-size shrink-only baseline drift, three unrelated landing/content admin tests) were already
documented in 050 and are not introduced or repaired here.

## 7. Change-path manifest

```text
A src/frontend/app/admin/shipping-services/shipping-services-roundtrip.test.ts
A docs/planning/shipping/SHIPPING-MVP-ADMIN-ROUNDTRIP-051.md
A docs/architecture/evidence/TB-SHIPPING-MVP-ADMIN-ROUNDTRIP-051/**
```

No production source file changed.

## 8. Limitations / outstanding risk

- The wire contract, path, method, DTO shape and all screen code exercised here are real, but responses
  came from an in-page fixture; the persistence of that same contract in Postgres remains covered by the
  existing backend tests, not by this run.
- The native `confirm` rendering itself was not exercised (the confirm boundary — exact message text and
  both answers — was).
- A durable in-repo React render test is not possible with the current harness; the durable equivalent is
  the fetch-level roundtrip test. If the Architect wants true component-render coverage, adding
  jsdom + React Testing Library (or Playwright) is a separate, dependency-adding decision.
- 050's remaining item is untouched: new services still accept free-text `code`/`providerKind` although
  the shipment path only accepts the five `ShippingMethodRegistry` codes — that belongs to the
  rate/coverage integration milestone.
