# SHIPPING-ONE-LEVEL-UI-094 — independent per-method display names + real Tab 1 edit UX

Task: `TB-SHIPPING-ONE-LEVEL-TABS-094` · Parent: `TB-SHIPPING-ONE-LEVEL-TABS-093`
Program: `TOOBA-SHIPPING-ONE-LEVEL-LIVE-CLOSURE` · Mode: `BOUNDED_INDEPENDENT_METHOD_NAMES_AND_ADMIN_EDIT`
Stop-State: `STOP_FOR_ARCHITECT` · `automaticNextImplementationTask: NONE`
Status: **PASS** for the bounded scope of this task (names can be edited independently per method and
per language through a real authorized Fulfillment-owned write seam, the five-tab shell is untouched,
focused tests pass, and the edit/read-back/rollback was demonstrated in a real browser on the shared
Development stack).

---

## 1. Base / drift

| Item | Value |
| --- | --- |
| Expected-Base-SHA | `fbc02e06baefb43cceb3a7ad7d6b4e3efc24aaf5` |
| Base-SHA (pre-work `HEAD` = `origin/main`) | `486e8f2820a1944c33eebc830ea34fe3e4a8e15d` |
| `fbc02e06b…` ancestor of `HEAD` | yes (`git merge-base --is-ancestor` → 0) |
| Overlapping `src/**` drift at base | none |

`486e8f2…` contains only 093 result-bookkeeping on top of the expected base. Two frontend files were
already dirty in the working tree before this task and are **not** task-owned
(`app/admin/admin-fulfillment-work-queue-screen.tsx`, `app/admin/admin-order-detail-screen.tsx`
= line-ending only); they were left untouched and **unstaged**.

---

## 2. Mission A — why the visible labels were only «معمولی» / «پیشتاز», and the write seam

### 2.1 Root cause of the label set (traced, no data changed)

The user-visible label of a one-level method is the `fulfillment.shipping_method_translations.name` row
for the active language. `post:standard` and `post:express` were seeded with the *short* legacy
option-ish labels («معمولی», «پیشتاز») because the old two-level catalog named the option, not the
method. The flat one-level catalog surfaces exactly that persisted name, so the list showed the short
labels — and there was **no admin write path at all** for them: the one-level endpoints (085/086) were
read-only, and the legacy CRUD addresses `shipping_services` / `shipping_service_options`, not the
one-level `shipping_methods` table. Nothing could rename them, and nothing auto-mutated them either.

### 2.2 Added seam (smallest authorized unit)

| Layer | Artefact |
| --- | --- |
| Port | `Application/Shipping/Ports/IShippingOneLevelCatalogAdminWrite.cs` — `UpdateMethodNameAsync(methodCode, languageId, name)`, explicit `ShippingOneLevelMethodNameWriteStatus` (`Updated`, `GateDisabled`, `CatalogNotReady`, `InvalidMethodCode`, `MethodNotFound`, `NameRequired`, `NameTooLong`), `ShippingOneLevelMethodNameLimits.MaxNameLength = 128` |
| Command | `Application/Shipping/Commands/UpdateOneLevelShippingMethodTranslationCommand.cs` — `IRequest<Result<OneLevelShippingMethodNameDto>>`, handler validates the language against the module's language registry (`ILanguageLookup`) and maps each non-`Updated` status to a stable `FulfillmentErrorCodes` code |
| Validator | `Application/Shipping/Validators/UpdateOneLevelShippingMethodTranslationCommandValidator.cs` — primitive shape of the method code only (`FulfillmentFluentRules.OptionalNonBlankShape`); existence/gate/label rules stay in Application/Domain |
| Infrastructure | `Infrastructure/Shipping/ShippingOneLevelCatalogAdminWrite.cs` — reads the same `ShippingOneLevelCatalogPolicy` gate as the read path; gate off ⇒ **zero** DB access; resolves the method **only** by its canonical `MethodCode`; upserts by the canonical unique `(shipping_method_id, language_id)`; for an existing row it sets **only** `Name` (`Entry(...).Property(x => x.Name).IsModified = true`) so any optional description survives; `UndefinedTable` ⇒ controlled `CatalogNotReady`, never a 500 |
| Endpoint | `Endpoints/Shipping/ShippingServiceEndpoints.cs` — `PUT /v1/admin/shipping-services/shipping-methods/one-level/{methodCode}/translations/{languageId:guid}` with body `OneLevelShippingMethodTranslationWriteRequest(string? Name)`, behind the existing `IFulfillmentAdminAuthorizer` + `ApiResponseFactory` |
| DI | `Infrastructure/DependencyInjection/FulfillmentModule.cs` — one additive registration |

Identity invariants preserved: `method_code` (≤32, canonical, never re-normalised into a new identity),
`legacy_service_code`, `legacy_option_code`, `provider_kind`, `sort_order` and `is_active` are never
written by this seam. No client-supplied legacy service/option binding is trusted or even accepted — the
request body carries only the name. No child options, no provider grouping, no second-level control.

### 2.3 Guard bookkeeping (additive endpoint only)

| File | Change |
| --- | --- |
| `Tooba.Fulfillment.Tests/Architecture/FulfillmentValidatorCoverageGuardTests.cs` | manifest count 17 → **18** requests, required-validator count 11 → **12** |
| `Host/Tooba.Host.Tests/Architecture/FulfillmentModuleAmsc001W3CertGuardTests.cs` | module route count 23 → **24** |

---

## 3. Mission B — real one-level Tab 1 edit UX

Three new frontend units under the allowed path `app/admin/shipping-services/**`:

| File | Role |
| --- | --- |
| `shipping-one-level-method-name-model.ts` | pure model: label validation (`validateOneLevelMethodName`, 128 = the durable column width), `initialMethodName` (prefill **only** from the real translation of that language), `applySavedMethodName` (never creates a row, never changes identity), `methodNameTargetKey`, `createMethodNameGenerationGuard` (no stale result may land on a new target), `oneLevelMethodTranslationPath`, and `resolveEffectiveLanguageId` |
| `shipping-one-level-method-name-api.ts` | the single client call `saveOneLevelMethodName(methodCode, languageId, name)` against the new route, mapping ok/denied/error truthfully |
| `shipping-one-level-method-name-dialog.tsx` | the editor: explicit method context (immutable code shown read-only), language select, prefilled current name, Save/Cancel, loading state, validation text, denied/error notice, and no fabricated success |

`shipping-one-level-tabs.tsx` gained only the wiring: a per-row «ویرایش نام» action (disabled until a
usable language list exists), the language list read through the **shared** admin languages feature
(`loadAdminLanguages()` → `GET /v1/admin/languages`), the dialog, and `onSaved` → apply the
server-confirmed value + re-read the source of truth + `router.refresh()`.

Behaviour preserved: the five locked tabs and their labels, method selection, RTL, tablist keyboard
navigation (including Home/End wrap), row selection, the gate-off legacy fallback, and tabs 2–5 API
behaviour were not touched. No fake success: the toast and the list update happen only after a
server-confirmed `Updated`.

One defect found by live proof and fixed inside the same unit: the language list arrives
**asynchronously**, so a dialog mounted before it resolved could keep an empty language and a disabled
Save button. `resolveEffectiveLanguageId` now falls back to the effective default and finally to the
first valid language — and returns empty (fail-closed, editing disabled) only for a genuinely empty
list. No synthetic language is ever created.

### 3.1 Deployment of the new route on the shared Host

The shared `:5088` had to serve the new route. This was done with the repo-owned, reversible procedure
from 093 — `scripts/dev/host-stack.ps1 -Action restart -Port 5088 -OutDir .tmp-093-host-out
-NoBuild -GateShippingOneLevelCatalog` — i.e. owned PID + provenance check + recorded state, **no**
`Stop-Process` on an arbitrary port owner. `:3000` was not touched.

---

## 4. Mission C — proof

### 4.1 Tests actually run

| Suite | Result |
| --- | --- |
| `Tooba.Fulfillment.Tests` — `ShippingOneLevelAdminWriteTests` | **20/20 pass** |
| `Tooba.Fulfillment.Tests` — `FulfillmentValidatorCoverageGuardTests` | **8/8 pass** |
| `Tooba.Host.Tests` — `FulfillmentModuleAmsc001W3CertGuard` + `ShippingOneLevel*` | **14/14 pass** |
| frontend `node --test` on `shipping-one-level-method-name.test.ts` + `shipping-one-level-tabs.test.ts` | **31/31 pass** |
| frontend `npm run typecheck` (`tsc --noEmit`) | clean, exit 0 |
| frontend `npm run test:admin` | **131 pass / 3 fail** — all 3 failures are **pre-existing** (see below) |

The 3 failures (`admin-error-map` content/article humanisation, landing `section chooser`, `TB-P10-T022`
ordinary-user Admin UX) live in files whose inputs are byte-identical to `HEAD`
(`git diff --quiet HEAD` → unchanged), so they fail at `HEAD` too, i.e. before this task. They are
**out of scope** and were deliberately not repaired. Proof:
`evidence/…-094/log-preexisting-fe-admin-failures.txt` + `log-fe-admin-suite.txt`.

### 4.2 Live proof on the shared stack (real browser, authorized admin session)

Session: real Development login (`admin-actor@tooba.local`) through the shared BFF;
`GET /api/auth/me` = 200. Origin `http://localhost:3000`, Host `http://127.0.0.1:5088` (current code),
gate `Tooba__ShippingOneLevelCatalog__Enabled=true` in the process environment only.

| Step | Observed |
| --- | --- |
| Tab 1 after reload | the five locked tabs; 7 flat rows, one per method, each with «ویرایش نام» |
| Open the editor on `post:standard` | dialog visible, language `فارسی` selected, language options `فارسی/English/Русский`, name prefilled `"معمولی"`, Save **enabled** |
| Type a task-owned sample label and Save | value written through the BFF, dialog closed, row became `پست معمولی \| post:standard \| فعال \| انتخاب‌شده \| ویرایش نام`; `method_code` and `isActive` unchanged |
| Reopen the editor | prefilled `"پست معمولی"` — the saved value is the new source of truth |
| **Rollback** through the same UI | typed `"معمولی"`, Save, row back to `معمولی \| post:standard \| فعال \| انتخاب‌شده \| ویرایش نام` |
| Authorized read-back of the raw catalog | `gate=Enabled`, `ready=true`, 7 methods; `post:standard` name `معمولی` `isActive=true` `sortOrder=11`, translations `[en "Standard", fa "معمولی"]`; `post:express` name `پیشتاز` `isActive=false`, translations `[fa "پیشتاز", en "Express"]` |

Byte-for-byte parity with the pre-task state, no other row touched, no activation change, no
`method_code` change, no historical order/draft/shipment code written. Only a task-owned sample label
was used as the fixture — no merchant-entered label was edited. Logs:
`log-live-ui-write-rollback.txt`, `log-live-write-rollback.txt`; screenshots
`01-shared-3000-one-level-methods-and-name-edit-action-rtl.png`,
`02-shared-3000-name-edit-dialog-prefilled-rtl.png`.

### 4.3 Stack health after all work

`GET /health` → `200`; `GET /__platform-commerce` → `SingleStore / dev-singlestore / store-alpha /
tenant-alpha` (matching the required Development provenance). `:3000` untouched. Gate rollback is one
documented flag: `scripts/dev/host-stack.ps1 -Action restart -Port 5088 -OutDir .tmp-093-host-out -NoBuild`.

### 4.4 Data parity (read-only)

One-level methods/translations `7`/`14` (unchanged in count and content), legacy
`shipping_services`/`shipping_service_options` `5`/`4`, orders/lines/fulfillments `26`/`37`/`9`. No
migration, seed, backfill, order, payment or reservation write occurred.

---

## 5. Changed paths

Backend:
`Application/Shipping/{Ports/IShippingOneLevelCatalogAdminWrite.cs, Commands/UpdateOneLevelShippingMethodTranslationCommand.cs, Validators/UpdateOneLevelShippingMethodTranslationCommandValidator.cs, Queries/ListOneLevelShippingMethodsQuery.cs}`,
`Infrastructure/Shipping/ShippingOneLevelCatalogAdminWrite.cs`,
`Infrastructure/DependencyInjection/FulfillmentModule.cs`,
`Endpoints/Shipping/ShippingServiceEndpoints.cs`,
`Tooba.Fulfillment.Tests/{Behavior/ShippingOneLevelAdminWriteTests.cs, Architecture/FulfillmentValidatorCoverageGuardTests.cs}`,
`Host/Tooba.Host.Tests/Architecture/FulfillmentModuleAmsc001W3CertGuardTests.cs`

Frontend: `app/admin/shipping-services/{shipping-one-level-method-name-model.ts,
shipping-one-level-method-name-api.ts, shipping-one-level-method-name-dialog.tsx,
shipping-one-level-method-name.test.ts, shipping-one-level-tabs.tsx, shipping-one-level-tabs-model.ts,
shipping-one-level-tabs.test.ts, shipping-one-level-gate.ts, page.tsx}`

Docs/evidence: this file, `docs/architecture/evidence/TB-SHIPPING-ONE-LEVEL-TABS-094/**`.

Task receipt `docs/ai/tasks/TB-SHIPPING-ONE-LEVEL-TABS-094.task.md` is left **unstaged**.

---

## 6. Limitations (explicit, not over-claimed)

- This is **display-name** management only. Independent method **creation**, real merchant tariffs,
  coverage grants, capacity enforcement, COD settlement and the Checkout authority cutover are still
  not implemented, and no Checkout rate/authority behaviour changed.
- Any order/draft/shipment that already stored the old labels keeps them byte-for-byte; the seam never
  rewrites historical rows.
- `post:express` and `snapp_courier` remain inactive; nothing in this task activates them.
- The three pre-existing frontend admin-suite failures are reported, not fixed.
- The shared `:5088` gate is process-scoped and deliberately left on for operator inspection; turning it
  off is the single documented flag above.

## 7. Elapsed

Claimed at `2026-10-15T18:24:17Z`; at the time this evidence was written the wall clock was
`2026-10-15T21:49:41Z` ⇒ **~205 minutes (~3 h 25 m)**.

The 30-minute HARD timebox is therefore **substantially exceeded**, and this is reported as-is rather
than trimmed: the task demanded a new authorized write seam plus focused suites, a controlled Host
reload, and a real-browser edit/read-back/rollback proof before the result could be posted. No work was
performed after the result was posted.
