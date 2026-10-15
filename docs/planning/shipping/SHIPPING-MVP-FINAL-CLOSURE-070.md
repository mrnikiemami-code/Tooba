# SHIPPING-MVP-FINAL-CLOSURE-070 — bounded option-level admin authoring + resolver closure

Task: `TB-SHIPPING-MVP-FINAL-CLOSURE-070` (parent `TB-SHIPPING-MVP-OPTION-PARITY-069`)
Program: TOOBA-SHIPPING-MVP-OPERATIONAL-CLOSURE · Mode: `BOUNDED_FINAL_RESOLVER_ADMIN_OPTION_AND_DELIVERY_GATE`
Expected base: `8351f66e550f89dc8f64d3efdc62ba1c138155d4` (verified ancestor of `HEAD`, `HEAD == origin/main` at claim)

## 1. Mission A — the 068/069 "legacy text destination gap" is CLOSED and NOT a Geography defect

### 1.1 What was claimed before

068 and 069 both reported that `IGeographyLookup.ResolveLegacyAsync("IR", "البرز", null, null)` returned a
non-`Resolved` outcome from the observer path, while a read-only SQL proof showed the country code `IR`
active and the `alborz` area carrying the translation label `البرز`. 069 handed this to 070 as a *named
blocker* precisely because the failing step had not been isolated.

### 1.2 Exact failing step — proven, not guessed

Two independent measurements, both against the real Development database `tooba_alpha`
(`127.0.0.1:5432`, container `postgres-db`, `postgres:17`).

**(a) The resolver itself is correct.** `docs/architecture/evidence/TB-SHIPPING-MVP-FINAL-CLOSURE-070/harness/`
constructs the real `GeographyPlaceLookup` over the real `GeographyDbContext` and calls the exact
observer signature (`log-resolver-trace.txt`):

```text
country id=e3451206-… code='IR' active=True
area code='alborz' active=True labels='البرز'[d8a7d984d8a8d8b1d8b2]
input 'البرز' bytes=d8a7d984d8a8d8b1d8b2   -> area code='alborz' labelMatch=True (only match)
ResolveLegacyAsync("IR","البرز",null,null) => Resolved count=1 administrativeAreaId=1f8050a8-c325-59fa-959f-82d76b5b2bc5
ResolveLegacyAsync("IR","alborz",null,null) => Resolved count=1 (same area)
ResolveLegacyAsync("IR","البرز","کرج",null)  => Resolved count=1 (city 68255340-… inside alborz)
```

**(b) The previous probes never sent the label.** The 068/069 matrix scripts (`probe-matrix.ps1`) built the
JSON body as a .NET **string** and handed it to `Invoke-RestMethod -Body`. A string body written without a
`charset` is encoded through a single-byte encoding, so every Persian code point collapses to `0x3f`:

```text
browser/JS UTF-8 body  bytes for 'البرز': d8 a7 d9 84 d8 a8 d8 b1 d8 b2
PowerShell latin1 body bytes for 'البرز': 3f 3f 3f 3f 3f   ('?????')
```

`ResolveLegacyAsync` is an **exact, normalised, non-fuzzy** comparison by design (043 semantics), so
`"?????"` correctly matches nothing and returns `Unresolved`. The observer then correctly reports
`DestinationNotComparable`. There was never a Geography bug.

### 1.3 Live end-to-end proof of the closure

One controlled Development restart with the shadow override enabled **in the process only**
(`Tooba__ShippingRateAuthority__ShadowEnabled` / `__PrerequisitesConfirmed`; no config file touched),
then seven projection requests through the real storefront route (`log-resolver-probe.txt`,
`log-observer-070.txt`):

| request | province value | encoding | observer outcome |
| --- | --- | --- | --- |
| P1 | `البرز` (label) | real UTF-8 bytes | `Observed` — 5/5 `Match` |
| P2 | `البرز` + `کرج` (labels) | real UTF-8 bytes | `Observed` — 5/5 `Match` |
| P3 | validated Geography ids | real UTF-8 bytes | `Observed` — 5/5 `Match` |
| P4 | `alborz` (stable code) | real UTF-8 bytes | `Observed` — 5/5 `Match` |
| P5 | `alborz` + `karaj` (codes) | real UTF-8 bytes | `Observed` — 5/5 `Match` |
| Q1 | `البرز` (label) | Latin-1 (the 068/069 defect) | `DestinationNotComparable` |
| Q2 | `البرز` + `کرج` (labels) | Latin-1 (the 068/069 defect) | `DestinationNotComparable` |

The per-method lines for the `Observed` cases are the 069 parity fixtures, all `Match`
(`post:standard 120000`, `tipax:express 260000`, `tipax:standard 180000`, `store_courier 75000`,
`in_person 0`). The five canonical per-method `Match` lines requested by this task are therefore captured
**from the Persian-label path itself**, not only from the validated-id path.

No source file in `Tooba.Geography.*` was changed: there is no demonstrated defect to fix, and inventing a
fuzzy/transliterating fallback would break the locked "exact match or refuse" semantics of 043/066.

## 2. Mission B — the real option-aware admin rate editor (frontend)

The 069 blocker was that the Shipping **admin API** already accepted an option dimension while the grid UI
could not author it. That gap is now closed inside the existing dialog — no new page, no new backend route.

| file | change |
| --- | --- |
| `shipping-rates-coverage-api.ts` | `ShippingRatePolicyWriteRequest.optionCode?` and `ShippingRatePolicyAdminItem.optionCode` added; `mapRate` reads camel/Pascal `optionCode`; both POST and PUT bodies now carry `optionCode` |
| `shipping-rates-coverage-model.ts` | new pure helpers: `ShippingServiceOptionChoice`, `SERVICE_LEVEL_OPTION_VALUE`, `rateOptionKey`, `rateOptionLabelFa`, `SERVICE_LEVEL_RATE_FA`, `validateRateOption`, `groupRatesByOption`, `SERVICE_OPTION_UNKNOWN_CODE` |
| `shipping-rates-coverage-dialog.tsx` | loads the real option list from the existing `GET /v1/admin/shipping-methods` tree, renders an option `<select>` in the create section for multi-option services, groups the policy list per option, shows each policy's immutable option identity, echoes the stored `optionCode` back on update, and maps `shipping.service_option.unknown` next to the option field |
| `shipping-rates-coverage-model.test.ts` | six focused tests for the option dimension |

Deliberate constraints honoured:

- the option list is **never invented and never parsed out of `methodCode`** — it is read from the existing
  Fulfillment service tree (`/v1/admin/shipping-methods`), and only codes that tree returns are offered;
- the empty selection means the **service-level policy**, so single-option and no-option services keep the
  exact current UX/semantics and the existing `optionCode = null` fallback;
- editing is **option-identity preserving**: the stored `optionCode` is echoed back in the PUT body, so a
  rate can never be silently reassigned to another option through this seam;
- the service-level fallback is labelled explicitly (`SERVICE_LEVEL_RATE_FA`);
- the coverage-grant editor is unchanged and stays service-scoped;
- the 066 stale-response guard and the D1/D2 destination-scope rules are untouched.

### 2.1 Focused tests (actually run)

```text
shipping-rates-coverage-model.test.ts          18/18 pass   (6 new option tests)
shipping-destination-picker.test.ts             3/3  pass
shipping-services-form-model.test.ts            9/9  pass
shipping-services-roundtrip.test.ts             6/6  pass
npx tsc --noEmit -p tsconfig.json               exit 0, zero diagnostics
```

## 3. Mission C — live Development roundtrip

Target proved positively (not remembered): listening socket `:5088` → `Tooba.Host.exe`, `/health` 200,
tenant `store-alpha`, `appsettings.Development.json` maps `tenant-alpha` → `Database=tooba_alpha`. The
user's frontend stayed up on `:3000` and was not restarted.

**Option source the new select reads** (`log-admin-option-roundtrip.txt`):

```text
GET /v1/admin/shipping-methods?language=fa => 200
  post          options=[standard]
  tipax         options=[express,standard]
  store_courier options=[]
  in_person     options=[]
TIPAX labels: express:پیشتاز | standard:معمولی
```

**Read**: `GET /v1/admin/shipping-services/tipax/rates` returns `optionCode` per policy
(`express 260000/lead1`, `standard 180000/lead3`, plus the superseded 068 service-level row still inactive).

**Edit**: `PUT …/rates/01a13d84-ca00-…-52803380de4e` with the exact payload the dialog builds (scope plus the
echoed `optionCode`) → 200, amount 180000 → 185000, **express stayed 260000** (no cross-option leakage),
option identity unchanged; then restored to 180000.

**Active toggle**: `PATCH …/active {"isActive":false}` → 200 then `{"isActive":true}` → 200.

**Refusal**: `POST …/rates` with `optionCode:"drone"` →
`400 {"errorCode":"shipping.service_option.unknown"}` — the exact code the UI now renders beside the field.

**Real UI evidence** (browser, not simulated): the tipax dialog renders
`shipping-create-rate-option` with options `سطح خدمت (پیش‌فرض خدمت) | پیشتاز | معمولی`, groups the rows by
option (`shipping-rate-option-group-service` / `-express` / `-standard`), and a genuine UI edit produced
`نرخ «معمولی · استان · …» ذخیره شد.` with the row showing `۱۸۷٬۵۰۰ IRR` while `پیشتاز` stayed
`۲۶۰٬۰۰۰ IRR`; a genuine UI toggle flipped `معمولی` inactive and back. Final UI state equals the documented
fixture ledger. Screens: `ui-tipax-option-dialog.png`, `ui-tipax-option-roundtrip.png`.

Legacy projection invariance: all seven shapes returned a byte-identical payload (2393 bytes each) with the
gate OFF and ON; prices and the method list shown to the customer are unchanged.

## 4. Mission D — delivery gate, safety, cleanup

- Shadow overrides were enabled **only** in the Development process and are **removed**: the restored host
  logs **zero** observer lines for the same projection, `/health` 200, listener `127.0.0.1:5088` pid 24364.
- No production/staging connection was made; no real order, payment or snapshot was modified.
- **No backend file changed**, so no backend suite was required; nothing was weakened and no baseline was
  widened. (A Geography test project does not exist in this repo — Geography is verified through evidence
  harnesses, which is the precedent used here.)
- Task-owned disposables were removed; the unrelated pre-existing dirty files
  (`admin-fulfillment-work-queue-screen.tsx`, `admin-order-detail-screen.tsx`,
  `TB-P0-INVENTORY-CONSUMPTION-JOURNAL-003/summary.md`) and the unstaged Bridge receipt were preserved and
  never staged.
- The Shipping master recovery checkpoint was **not** edited: a bounded edit could not be guaranteed in the
  remaining timebox, so full closure is recorded here as the task explicitly permits.

## 5. Fixture ledger (synthetic, DEVELOPMENT-ONLY, area `alborz` scope)

| service | option | ratePolicyId | charge | lead | state |
| --- | --- | --- | --- | --- | --- |
| `tipax` | `standard` | `01a13d84-ca00-7000-8d86-52803380de4e` | 180 000 | 3 | active |
| `tipax` | `express` | `01a13d84-ca7f-7000-a59a-269a1c8ec13d` | 260 000 | 1 | active |
| `tipax` | *(service level, superseded)* | `01a13d65-cf37-7000-ad21-ac8e9b2c89d7` | 180 000 | 3 | inactive |
| `tipax` | — | grant `01a13d65-cf8e-7000-a8e5-2205f6167d63` | — | — | active |

Values are parity fixtures reproducing *measured legacy* amounts — **not approved commercial tariffs**.

## 6. DELIVERY VERDICT (five independent categories)

| # | category | verdict |
| --- | --- | --- |
| 1 | Admin authoring / UI ready | **YES** — option-aware create/read/edit/toggle proven live in the UI and over the API |
| 2 | Geography identity ready | **YES** for the tested scope — exact Persian label, stable code and validated ids all resolve to the same real identity; the 068/069 gap was a probe-encoding artefact, now closed with positive evidence |
| 3 | Shadow parity on representative tested data | **YES on the tested fixtures** — 5/5 `Match` on the Alborz area fixtures, five separate times |
| 4 | Commercial tariffs & destination coverage approved/complete | **NO** — fixtures are synthetic comparisons, only one area is covered, and no tariff has product approval |
| 5 | Customer authority cutover authorized/ready | **NO** — `ShadowEnabled` / `PrerequisitesConfirmed` are OFF, no cutover was performed, and legacy pricing authority is untouched |

```text
FULL COMMERCIAL READY = NOT CLAIMED
Absent gate: explicit product/commercial approval of real tariffs and complete destination coverage,
plus an authorized cutover decision. This task is not commercial approval.
```

The legacy-safe MVP remains functional and unchanged: this task changed the admin authoring surface and
documentation only — no pricing authority, no checkout path, no customer-facing behaviour.
