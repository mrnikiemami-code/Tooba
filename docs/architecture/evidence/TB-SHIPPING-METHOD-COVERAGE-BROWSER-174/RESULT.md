# TB-SHIPPING-METHOD-COVERAGE-BROWSER-174 — Worker Result

**Task:** `TB-SHIPPING-METHOD-COVERAGE-BROWSER-174`
**Parent:** `TB-SHIPPING-ADMIN-METHOD-COVERAGE-UX-173`
**Protocol:** `BRIDGE-WAKE-V1` · **Channel:** `tooba-main` · **Status:** PASS
**Base / HEAD:** `db80a0231ece17a6da3cf30ab4728d98509d67ba` (`HEAD == origin/main` before the run, equal to `Expected-Base-SHA`)
**Production source changes:** none (the TB-173 surface was exercised unchanged; no UI defect required a fix)

## Mission result

Closed both honest TB-173 acceptance gaps with a real authorized Admin browser session and a real
authenticated storefront cart, against a disposable loopback stack:

1. In the real Admin UI (`/fa/admin/shipping-services`), `tipax:standard` was moved from the default
   **بدون محدودیت مدیریتی (Unrestricted)** to **محدود به مناطق منتخب (Restricted)** with exactly one
   method-owned destination grant (Iran / Tehran), saved through the UI, proven in SQL, then verified
   again after a full document reload. View mode was confirmed read-only. The same method was then
   returned to **Unrestricted** through the UI; a second full reload re-opened it as Unrestricted with
   the historical grant still present in the database (auditable, not inherited by the sibling).
2. For the same authenticated cart, the strict `ShippingOwnedRateAuthority` projection priced the
   method at its own owned rate when allowed, excluded it at a rated-but-ungranted destination while
   `tipax:express` stayed priced and unchanged, rejected an unsupported method-selection `PUT` with
   `400 shipping.method.unavailable` without corrupting the previously accepted draft, and priced the
   method again after the UI restore to Unrestricted.

No coverage, price, city, carrier or slot was invented. No production gate, schema, checkout, payment,
settlement or other Admin surface was touched.

## Isolation (verified before any mutation)

Isolation guard: `isolation-guard-on-174.json`

- disposable PostgreSQL container `tooba-tb174-pg` on `127.0.0.1:55494`
  (`tooba_174_scratch`, messaging `tooba_174_messaging`, user `scratch174`)
- isolated Development Host on `127.0.0.1:5428`
- private Next frontend on `127.0.0.1:3123` pointed at the isolated Host
- `ShippingOwnedRateAuthority` enabled **only** in the disposable scratch overlay
  (`.tmp-174-run/appsettings.Development.json`); committed `appsettings*.json` remain `"Enabled": false`
- guard confirms `containsSharedAlphaDatabase=false`, `containsSharedPort5432=false`,
  `sharedFrontendPort3000Touched=false`, `sharedHostPort5088Touched=false`, `failClosed=true`,
  single scratch tenant `store-alpha:Active:tenant-174-scratch`
- shared `:3000`, `:5088`, `:5432`, `tooba_alpha`, real `store-alpha` data and other worker services
  were never restarted or modified

Scratch fixture (`scratch-174-geography-rates-fixture.sql`): real Iran / Tehran / Karaj geography,
two distinct owned options of one provider, method-owned active rate rules:

| option | D1 (Tehran) | D2 (Karaj) |
|---|---|---|
| `tipax:standard` | 205,000 IRR | 210,000 IRR |
| `tipax:express` | 480,000 IRR | 490,000 IRR |

Authenticated customer: genuine Development OTP fixture, single-seller cart with two real offer lines
(`session-174.json` is scratch-only and **not** committed; no token/OTP/secret is present in the
committed evidence).

## Admin UI evidence (real browser, no mocks)

DOM assertions captured live: `admin-ui-dom-assertions-174.json`

| Step | Observation | Screenshot |
|---|---|---|
| method detail | exactly four non-nested tabs: عمومی / تعرفه و پوشش / برنامه کاری حمل / بسته‌بندی و کرایه; RTL; server-sourced binding «روش یک‌سطحی tipax:standard → سرویس tipax · standard»; three row actions only | — |
| Restricted selected | `Restricted` radio checked, one destination Iran/Tehran chosen; explicit copy that the mode is geographic restriction only | `admin-coverage-restricted-selected-174.png` |
| Restricted saved | saved through the UI, Restricted still selected, one allowed destination for this method only | `admin-coverage-restricted-saved-174.png` |
| View mode after full reload | opened via مشاهده; both radios, save button, destination selectors, rate inputs and add-rate button disabled | `admin-coverage-view-mode-readonly-174.png` |
| Restored Unrestricted | Unrestricted radio checked and saved; restriction controls disabled after commit | `admin-coverage-restored-unrestricted-174.png` |
| Re-opened after full reload | Unrestricted checked, editable, server read-back «بدون محدودیت مدیریتی: ادمین محدودیتی برای این روش تعریف نکرده است.» | `admin-coverage-unrestricted-after-reload-174.png` |

## Authoritative SQL read-back (disposable scratch DB only)

```
-- immediately after the Restricted save
declaration: (tipax, standard, mode=1)  created_at 2026-10-17 06:26:26.019602+00
own grant:   tipax/standard country=IR administrative_area=THR city=NULL is_active=true
sibling:     no declaration and no grant for tipax:express

-- after the UI restore to Unrestricted
declaration: (tipax, standard, mode=0)  updated_at 2026-10-17 06:28:17.820063+00
grant rows:  exactly 1, still tipax/standard (history retained, not deleted, not copied)
sibling:     still 0 declarations / 0 grants
```

`mode` check constraint allows `0|1` only; one declaration row per `(service_code, option_code)`.

## Customer strict-quote acceptance (same authenticated cart)

Harness: `acceptance-174.js` → `assertion-matrix-{baseline,restricted,unrestricted}-174.json`,
`http-trace-{baseline,restricted,unrestricted}-174.txt`, projections
`projection-*.json`. **22/22 checks PASS** (8 + 9 + 5).

Baseline (no declaration):

- D1 Tehran: `tipax:standard=205000`, `tipax:express=480000`
- D2 Karaj: `tipax:standard=210000`, `tipax:express=490000`
- one real selection accepted (`tipax:express`, server-returned schedule, 480,000) and persisted

Restricted + own Tehran grant:

- D1: `standard=205000` still present; `express=480000` unchanged
- D2: `standard` absent from the projection while `express=490000`; no zero/free fallback appears
- exclusion is coverage, not a missing rate: the active D2 rate row for standard still exists
- unsupported selection `PUT` → **400 `shipping.method.unavailable`**
- prior accepted draft unchanged afterwards: `selected=tipax:express`, amount `480000`
- exactly one declaration row and one active own grant; sibling untouched

Restored Unrestricted:

- D2 prices `standard=210000` again, D1 `standard=205000`
- `express=490000` unchanged
- own historical grant retained (1 row), sibling grant count 0

## Tests

No production source file was changed, therefore no typecheck or test suite was re-run (the task
allows focused tests only if source changes). Verification is the real browser write/reload/read-back,
SQL read-back and the authenticated HTTP acceptance above.

## Files changed (this task only)

- `docs/ai/tasks/TB-SHIPPING-METHOD-COVERAGE-BROWSER-174.task.md` (authoritative downloaded artifact)
- `docs/architecture/evidence/TB-SHIPPING-METHOD-COVERAGE-BROWSER-174/`
  - `prepare-isolated-target-174.js`, `bootstrap-scratch-174.ps1`,
    `scratch-174-geography-rates-fixture.sql`, `acceptance-174.js`, `cleanup-174.ps1`,
    `post-result-174.js`
  - `admin-ui-dom-assertions-174.json`, assertion matrices, HTTP traces, projections,
    isolation guard, host launch log, cleanup transcript, this result, `RESULT.bridge.txt`
  - five genuine browser screenshots (listed above)
- no `src/**` change; the pre-existing dirty `src/frontend/app/admin/admin-fulfillment-work-queue-screen.tsx`
  (last written 2026-10-16) and pre-existing dirty TB-173/075/079 evidence were left untouched and
  **not** committed

## Cleanup

`cleanup-174.txt`:

```
HOST_PROCESSES_STOPPED=true
CONTAINER=tooba-tb174-pg
CONTAINER_REMAINING=none
SHARED_PORTS_3000_5088_5432_TOUCHED=false
```

The private Next dev process on `:3123` was stopped by exact PID/command line after teardown.
Post-cleanup probe: `55494`, `5428`, `3123` closed; `5432`, `5088`, `3000` still listening and untouched.

## Gaps / unproven (disclosed)

- The 401/403 denial path for coverage writes was not re-exercised in this browser run (covered by
  TB-173); this run used the sanctioned Development Admin actor.
- Customer inclusion/exclusion was proven through the authenticated customer projection/quote and the
  method-selection `PUT`; no storefront payment/checkout step was executed (out of scope, forbidden).
- The exclusion reason is observed as method absence plus `400 shipping.method.unavailable` on
  selection; no additional reason payload beyond the existing API contract was asserted.

**STOP_FOR_ARCHITECT: YES** — await ACCEPT / REPAIR / BLOCK. No next task started.
