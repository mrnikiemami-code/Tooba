# TB-SHIPPING-ADMIN-MULTILINGUAL-ACTIVATION-169 — evidence

Mode: `BOUNDED_REAL_ADMIN_MULTILINGUAL_AND_ACTIVATION_ACCEPTANCE` (acceptance only, no production code change).

Verdict: **PASS** (per-language names persisted across save → GET → reopen → reload; sibling
isolation clean; activation/deactivation truthful with no fake readiness).

Base-SHA: `814ae785618b45512f51a661d40a7ef1047db367`
Code-SHA: `NONE` (acceptance-only task; no source file changed)
Expected-Base-SHA: `814ae785618b45512f51a661d40a7ef1047db367`

## 1. What TB-148 left open, and what this task closed

TB-148 shipped the multilingual Admin UI but its evidence covered model/unit behaviour. TB-167/168
shipped the canonical one-level method create contract and its isolated runtime create acceptance.
Neither had proven, in a real authorized browser against a real PostgreSQL:

- a per-language name save through the actual General tab, then authoritative GET, modal reopen and
  a full document reload;
- draft isolation when switching language with an unsaved draft;
- truthful activation/deactivation of a **newly created flat method** (which by construction has no
  legacy service/option row) — refusing instead of faking readiness.

All three are now proven. No source file was modified.

## 2. Source inspection before testing (owner-correct, no redesign)

| Surface | Owner | Inspected |
| --- | --- | --- |
| `PUT /v1/admin/shipping-services/shipping-methods/one-level/{methodCode}/translations/{languageId}` | Fulfillment (`ShippingServiceEndpoints`, 094 seam) | Yes — only allowed name write; language id must exist in the Localization registry |
| `PUT /v1/admin/shipping-services/shipping-methods/one-level/{methodCode}/activation` | Fulfillment (`ShippingOneLevelCatalogActivation`, 096 seam) | Yes — resolves legacy binding from the **method row only**; `LegacyOptionMissing` ⇒ `fulfillment.shipping_method.unsupported` |
| `GET …/one-level` and `GET …/one-level/{methodCode}/binding` | Fulfillment | Yes |
| `src/frontend/app/admin/shipping-services/flat-method-language-editor.tsx` | Frontend | Yes — per-language draft map, generation guard, success only from server echo for the same target |
| `flat-general-tab.tsx` / `shipping-one-level-activation-model.ts` | Frontend | Yes — eligibility only from resolved server binding; no optimistic/fake success |

## 3. Isolation (fail-closed)

- Task-owned container `tooba-tb169-pg` (postgres:16-alpine, `127.0.0.1:55488`), databases
  `tooba_169_scratch` + `tooba_169_messaging`, user `scratch169` (password from environment only,
  never in evidence).
- Private Host `127.0.0.1:5423` (SingleStore, single tenant `store-alpha` → `tenant-169-scratch`,
  Authorization `InMemory`, `ShippingOneLevelCatalog.Enabled=true` only in the scratch overlay).
- Private Next frontend `127.0.0.1:3118` → private Host.
- Shared `:3000` / `:5088` / `:5432`, `tooba_alpha`, shared store data, RabbitMQ and Redis untouched.
- `isolation-guard.json` → `failClosed=true` (one active tenant, no alpha reference, no shared port,
  no `ShippingRateAuthority` / `ShippingOperationalShadow`, no edition swap).
- Canonical migrations applied to the disposable database by the product's own
  `Tooba.MigrationRunner apply --all-tenants` (exit 0).

## 4. HTTP acceptance — 27/27 PASS (`assertion-matrix-http-169.json`, `http-trace-169.txt`)

Real authorized Development actor from the Host's own dev-context seam; capabilities from the
product's own access-control bootstrap (`fulfillment.manage` present, 55 grants). Language ids are
the live registry ids (`registry-languages-169.json`):

| code | languageId |
| --- | --- |
| fa-IR | `01a1483f-df34-7000-806c-11c0fe310dcc` |
| en-US | `01a1483f-df34-7000-8bd5-38f3f5eb0b7f` |

Highlights:

- `tipax:scratch169` created through the canonical one-level POST (200), server-derived binding
  `(tipax, scratch169)`, `isActive=false`.
- `PUT …/translations/{fa}` and `…/{en}` both 200; authoritative GET returns both names for **both**
  list languages (the response carries the full translation set).
- Negatives fail closed and change nothing: unknown language 400 `shipping_service.language_invalid`,
  blank name 400 `shipping_method.name.required`, anonymous 401 `admin.actor.missing`, ungranted 403
  `admin.authorization.denied`.
- Activation of the new method: **400 `fulfillment.shipping_method.unsupported`**, row remains
  inactive — the exact, honest limitation (no legacy option row exists by construction).
- Existing fully bound sibling `tipax:express`: deactivate 200 `{isActive:false, changed:true}` then
  reactivate 200 `{isActive:true}`; `tipax:standard` and the new method unaffected.

## 5. Real browser acceptance (private Admin UI on `:3118`)

1. Grid shows 8 independent rows with exactly three row actions (مشاهده / ویرایش / حذف); the new
   method appears as `تیپاکس چندزبانه ۱۶۹` / `tipax:scratch169` / status `خیر`.
2. Edit dialog opens with the four non-nested tabs (عمومی، تعرفه و پوشش، برنامه کاری حمل،
   بسته‌بندی و کرایه). Language editor lists exactly the two registry languages, both `ثبت‌شده`.
3. English selected, draft `Unsaved Draft 169` typed **without saving**; switching to فارسی shows the
   saved Persian name `تیپاکس چندزبانه ۱۶۹` (no contamination) and the English row marked
   `پیش‌نویس ذخیره‌نشده`; switching back restores the draft text intact.
4. English name saved through the General tab (`ذخیرهٔ نام این زبان`): server-echoed success
   `نام روش در زبان انتخاب‌شده به «Tipax UI Saved 169» ذخیره و دوباره خوانده شد.` and the panel's
   authoritative value updated to `Tipax UI Saved 169`.
5. Activation attempt on the new method through its own UI control: alert
   `این عملیات برای این روش پشتیبانی نمی‌شود؛ فعال‌سازی سطح سرویس بدون گزینه هنوز مجاز نیست.`
   Status stays `غیرفعال` — no fake success, no financial readiness implied.
6. Full document reload, then modal reopen: both languages `ثبت‌شده`, Persian name and
   `Tipax UI Saved 169` present, activation still `غیرفعال`, four tabs intact.
7. Independent activation cycle on the existing fully bound `tipax:express` through the same UI:
   notice `روش «پیشتاز» غیرفعال شد.` with grid status `خیر`, then `روش «پیشتاز» فعال شد.` with
   `بله`; `tipax:standard` stayed `بله` and `tipax:scratch169` stayed `خیر` throughout.

Screenshots: `ui-general-tab-en-saved-169.png`, `ui-activation-refused-169.png`,
`ui-sibling-activation-cycle-169.png`.

## 6. Authoritative SQL read-back (`db-readback-169.txt`)

```
method_code      | is_active
tipax:express    | t
tipax:scratch169 | f
tipax:standard   | t

tipax:scratch169 | fa-IR | تیپاکس چندزبانه ۱۶۹
tipax:scratch169 | en-US | Tipax UI Saved 169
tipax:standard   | fa-IR | معمولی     | en-US | Standard
tipax:express    | fa-IR | پیشتاز     | en-US | Express

legacy_option_rows=0  rate_policies=0  coverage_grants=0
```

Sibling translations and activation states are byte-identical to the pre-create baseline; the new
method owns zero legacy service/option/rate/coverage rows, so it can neither be activated nor made
customer-selectable.

## 7. Cleanup

`teardown-169.txt`: `SCRATCH_PORTS_CLOSED=True`, `SCRATCH_CONTAINER_REMOVED=True`,
`RUN_DIRECTORY_REMOVED=True`, `SHARED_PORTS_INTACT=True`, `SHARED_5432_TOUCHED=False`,
`SHARED_5088_TOUCHED=False`, `SHARED_3000_TOUCHED=False`.

## 8. Open gap (explicit, not claimed as ready)

A newly created flat method has **no** legacy `shipping_services` / `shipping_service_options` row
and therefore cannot be activated by the existing 096 activation seam
(`LegacyOptionMissing` → `fulfillment.shipping_method.unsupported`). This is intentional upstream
behaviour (the storefront eligibility filter reads the legacy option), not a defect introduced here,
and no provider/option entity or price was synthesized. New-method operational readiness therefore
remains an **OPEN GAP** owned by a future task that must decide (Architect-level) how independent
methods obtain a real, non-fabricated customer eligibility source.

## 9. Pre-existing / unrelated

No test suite was run beyond the focused acceptance; no pre-existing failure was observed in the
touched surface because no source file changed. Unrelated dirty/stashed files and historical
evidence were preserved.
