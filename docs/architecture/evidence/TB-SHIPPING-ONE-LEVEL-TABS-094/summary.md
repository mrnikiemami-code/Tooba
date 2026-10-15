# TB-SHIPPING-ONE-LEVEL-TABS-094 — evidence summary

Task: `TB-SHIPPING-ONE-LEVEL-TABS-094` · Parent: `TB-SHIPPING-ONE-LEVEL-TABS-093`
Mode: `BOUNDED_INDEPENDENT_METHOD_NAMES_AND_ADMIN_EDIT` · Status: **PASS** (bounded scope)
Stop-State: `STOP_FOR_ARCHITECT` · automaticNextImplementationTask: `NONE`
Plan: `docs/planning/shipping/SHIPPING-ONE-LEVEL-UI-094.md`

| Item | Value |
| --- | --- |
| Expected-Base-SHA | `fbc02e06baefb43cceb3a7ad7d6b4e3efc24aaf5` |
| Base-SHA | `486e8f2820a1944c33eebc830ea34fe3e4a8e15d` |
| Base is ancestor | yes |
| Bridge task row | `9a41c065-6112-4f80-9917-6c19093b9aa7` |

## What was delivered

1. **Fulfillment-owned admin write seam** (smallest authorized unit): `IShippingOneLevelCatalogAdminWrite`
   + `UpdateOneLevelShippingMethodTranslationCommand` (MediatR, `ISender`) + validator + EF
   implementation + one additive module route
   `PUT /v1/admin/shipping-services/shipping-methods/one-level/{methodCode}/translations/{languageId:guid}`,
   behind the existing admin authorizer and `ApiResponseFactory`. Gate-off ⇒ zero DB access; unknown
   language/method, blank or oversized label ⇒ explicit fail-closed result; upsert only by the canonical
   `(shipping_method_id, language_id)`; only the `Name` column is written.
2. **Real Tab 1 edit UX** on «سرویس‌ها»: per-method «ویرایش نام» action, dialog with language choice,
   prefilled real translation, Save/Cancel, loading, validation, denied/error feedback and stale-result
   protection; on success the source of truth is re-read (`load()` + `router.refresh()`), never a
   fabricated success. Five tabs, RTL, keyboard navigation, selection and tabs 2–5 untouched.
3. **Proof**: focused backend/frontend suites green, `npm run typecheck` clean, and a real-browser
   authorized edit → read-back → rollback on the shared Development stack.

## Tests actually run

| Suite | Result |
| --- | --- |
| `Tooba.Fulfillment.Tests` `ShippingOneLevelAdminWriteTests` | 20/20 pass |
| `Tooba.Fulfillment.Tests` `FulfillmentValidatorCoverageGuardTests` | 8/8 pass |
| `Tooba.Host.Tests` `FulfillmentModuleAmsc001W3CertGuard` + `ShippingOneLevel*` | 14/14 pass |
| frontend `node --test` (shipping-services 094 + tabs tests) | 31/31 pass |
| frontend `npm run typecheck` | clean |
| frontend `npm run test:admin` | 131 pass / 3 fail — **3 pre-existing, out of scope** |

## Live proof (shared Development stack, real browser)

`http://localhost:3000/fa/admin/shipping-services` (real `admin-actor@tooba.local` login, `/api/auth/me`
= 200) against `http://127.0.0.1:5088` (current code, gate on in the process environment only).

- Editor prefilled `"معمولی"` for `post:standard` / `فارسی`, Save enabled.
- Saved a **task-owned sample label** «پست معمولی» → row updated, `method_code`/`isActive` unchanged.
- Reopened: prefilled with the new saved value (source of truth updated).
- **Rolled back** through the same UI to `"معمولی"` → byte-for-byte parity restored.
- Authorized read-back: `gate=Enabled`, `ready=true`, 7 methods; `post:standard` = `معمولی`/active,
  `post:express` = `پیشتاز`/inactive; no other row touched.

## Files in this directory

| File | Content |
| --- | --- |
| `summary.md` | this file |
| `RESULT.bridge.txt` | the exact Worker Result contract posted to Bridge |
| `post-result.js` | the one-shot poster used after commit/push (no probe payloads) |
| `task-receipt.json` | the persisted downloaded `.task.md` receipt |
| `log-fe-admin-suite.txt` | raw `npm run test:admin` run |
| `log-preexisting-fe-admin-failures.txt` | proof the 3 admin-suite failures are pre-existing |
| `log-live-ui-write-rollback.txt` | real-browser edit / read-back / rollback |
| `log-live-write-rollback.txt`, `log-live-write-rollback-2.txt` | earlier HTTP-level authorized write/read-back/rollback |
| `log-host-stack-restart.txt` | controlled reload of `:5088` with the repo-owned script |
| `log-final-operator-state.txt`, `log-final-operator-state-094.txt` | final stack/gate/data state + rollback command |
| `01-shared-3000-one-level-methods-and-name-edit-action-rtl.png` | Tab 1 rows + «ویرایش نام» |
| `02-shared-3000-name-edit-dialog-prefilled-rtl.png` | editor prefilled from the real translation |

## Rollback

- Label rollback: already executed and verified (see `log-live-ui-write-rollback.txt`).
- Gate rollback:
  `powershell -File scripts/dev/host-stack.ps1 -Action restart -Port 5088 -OutDir .tmp-093-host-out -NoBuild`
- No migration/seed/backfill was added, so there is no schema rollback.

## Limitations

Display-name management only: no independent method creation, no Checkout authority/rate cutover, no
capacity/COD/delivery-promise work, inactive methods stay inactive, historical rows keep their old
labels, and the 30-minute timebox was exceeded (~205 minutes from claim to result) as reported.
