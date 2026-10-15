# SHIPPING-ONE-LEVEL-UI-087 — tab 3 «بسته‌بندی و کرایه»

Task: `TB-SHIPPING-ONE-LEVEL-TABS-087` (parent `TB-SHIPPING-ONE-LEVEL-TABS-086`)
Mode: `BOUNDED_ONE_LEVEL_PACKAGING_AND_FREIGHT_TAB`
Stop-State: `STOP_FOR_ARCHITECT`
Expected-Base-SHA: `db3849895f2f8611ba49970ba836b7e13ee12dff`

## 1. Scope actually delivered

Tab 3 of the default-OFF one-level shell is now a real, working packaging-and-freight surface for
**exactly one** selected one-level method. It reuses the existing Shipping-owned packaging-policy
admin APIs (075/077) and the proven packaging presentation/validation model unchanged. It adds **no
backend source at all**: the 086 binding resolver already provides everything tab 3 needs.

It does not present a provider/option hierarchy, does not infer the legacy binding by splitting
`methodCode`, does not enable any gate, and does not touch customer Checkout.

## 2. Mission A — single-level binding and operations

The method→binding authority is the 086 resolver, reused as-is:

```
GET /v1/admin/shipping-services/shipping-methods/one-level/{methodCode}/binding
  -> { gate, ready, resolved, methodCode, serviceCode, optionCode, isActive }
```

| Rule | How it is enforced |
| --- | --- |
| No browser-side splitting of `method_code` | The tab only ever calls `oneLevelBindingPath(methodCode)`; no `split(":")` anywhere in the component or model (asserted by test). |
| No client-provided arbitrary binding | The write target is produced only by `packagingWriteTarget(resolvedBinding)`; the tab has no editable service/option input. |
| No guessed pair on missing/unresolved/disabled binding | `resolvePackagingState` returns `binding-unavailable` for `gate-off`, `not-ready`, `unresolved`, `untrusted`, and the component renders an explicit refusal instead of any list or form. |
| Policies scoped to the selected method only | `scopePackagingToBinding` matches `serviceCode` **and** `optionCode` exactly (null matches null). A service-level fallback never appears under an option-bearing method and vice-versa. |
| Fallback vs option semantics intact | `optionCode` stays nullable: for `store_courier`/`in_person` the service-level policy *is* the method's policy; for `post:standard` the service-level policy is not shown at all. |
| No second selectable level | No option `<select>`, no `groupPackagingByOption`, no `packagingOptionLabelFa`, no `loadShippingMethodTree` (asserted by test). |
| Reused validation/localization | Fee parsing (`decimal(18,2)` exact, no silent rounding), currency normalization, payer-mode contract and every Persian refusal message come from `shipping-packaging-model.ts`. |
| No ledger / charge collection / checkout pricing | Nothing here creates a Payment or settles a collect-on-delivery amount; the UI states `PAYER_MODE_IS_QUOTE_FACT_FA` explicitly. |
| Stale selection | `createBindingGenerationGuard` bumps on every method change; `operationStillTargetsMethod` keeps a completion notice tied to the identity that started the operation, and the post-write refresh is skipped when the method changed. No cancellation of a committed network operation is claimed. |

A mismatched read response (`packagingViewMatchesBindingService` false) is refused before anything is
presented or mutated, so policies of another service can never be edited from this method's context.

## 3. Mission B — merchant UX and honest status

`shipping-one-level-packaging-tab.tsx`:

* **Method context** banner shows the selected method and its resolved legacy pair.
* **Policy list** (one level): destination scope summary, fee + currency, freight payer
  (پیش‌کرایه/پس‌کرایه), active state.
* **Create**: destination via the existing Geography-only `ShippingDestinationPicker`, fee, currency,
  payer mode. Option identity comes from the server binding.
* **Edit**: fee, currency and payer mode. Scope and option identity are echoed back unchanged so a
  policy can never be silently moved to another option or destination.
* **Activate / deactivate**: existing `PATCH …/{id}/active` client.
* **Required states**: no selected method; binding loading; binding unavailable (gate OFF / not
  migrated / unresolved / invalid response); policies loading; empty (explicit "fee is zero"); 403
  unauthorized; unexpected error; service mismatch; busy/saving; success notice.
* **RTL / keyboard**: five-tab URL model, `tablist`/`tab`/`tabpanel`, roving `tabIndex` and arrow keys
  from 084 untouched. Tabs 1 and 2 are preserved; placeholders 4 and 5 remain placeholders.
* The legacy `ShippingPackagingDialog` and its row actions are **not** removed and remain the gate-OFF
  route; `/admin/shipping-services` stays fully usable.

## 4. Changed paths

Frontend only:
* `src/frontend/app/admin/shipping-services/shipping-one-level-packaging-model.ts` (new)
* `src/frontend/app/admin/shipping-services/shipping-one-level-packaging-tab.tsx` (new)
* `src/frontend/app/admin/shipping-services/shipping-one-level-packaging-tab.test.ts` (new)
* `src/frontend/app/admin/shipping-services/shipping-one-level-tabs.tsx` (tab-3 wiring)
* `src/frontend/app/admin/shipping-services/shipping-one-level-tabs-model.ts` (tab-3 wiring task → null)
* `src/frontend/app/admin/shipping-services/shipping-one-level-tabs.test.ts` (wiring assertion)

Docs:
* `docs/planning/shipping/SHIPPING-ONE-LEVEL-UI-087.md`
* `docs/architecture/evidence/TB-SHIPPING-ONE-LEVEL-TABS-087/**`
* `docs/ai/tasks/TB-SHIPPING-ONE-LEVEL-TABS-087.task.md` (receipt, kept UNSTAGED)

**No backend source changed.** The existing packaging APIs (`GET/POST /packaging-policies`,
`PUT …/{id}`, `PATCH …/{id}/active`) already cover read/create/update/activation, so no new Shipping
endpoint was invented and the allowed-path widening described in the task was not needed.

## 5. Tests and results

| Command | Result |
| --- | --- |
| `node --test --experimental-strip-types app/admin/shipping-services/shipping-one-level-packaging-tab.test.ts` + `…-tabs.test.ts` | 26 passed / 0 failed |
| `node --test --experimental-strip-types "app/admin/shipping-services/*.test.ts"` | 154 passed / 0 failed |
| `npm run typecheck` (frontend) | clean |

Backend tests were **not** run: no backend source changed, so per Mission C.8 only the narrow frontend
files and typecheck were executed. The known Host DLL lock (Tooba.Host pid 33548) was not re-challenged
and no service was restarted.

Focused coverage added: `post:standard`, `tipax:express`, optionless `store_courier`/`in_person`,
inactive/mismatched rows, unmapped binding, policy-option filtering and nullable fallback, service
mismatch refusal, the full state machine (never `ready` without a resolved binding), stale-operation
identity, explicit-zero empty text, Prepaid vs CollectOnDelivery payload/display (inherited unchanged
from the proven packaging model), and source guards proving no option picker, no provider hierarchy,
no method-code splitting and no settlement/checkout reference.

## 6. Gate / database truth

* Gate `Tooba:ShippingOneLevelCatalog`: **OFF by absence**. Not switched by this task.
* Development one-level migration/backfill: **unapplied**. The running Host may predate the 086 route.
* Therefore: **source-and-test integration only**. Tab 3 is not claimed to be deployed, and the system
  is not claimed to be commercially ready. No live browser CRUD or database success is asserted.
* No DDL/DML, no seeding, no service activation, no Host/Next restart, no Checkout/Payment/Settlement
  change, no capacity reservation, no delivery promise, no invented weight or item counts.

## 7. Blockers and open prerequisites

1. Live tab-3 CRUD requires the gate to be enabled **and** the authorized one-level migration to run —
   a separate, unauthorized task. Until then the tab truthfully reports the binding as unavailable.
2. The 086 Host cert-guard run remains blocked while `Tooba.Host` (pid 33548) holds its output DLLs;
   not re-challenged here.
3. Legacy packaging dialog remains the operational surface while the gate is OFF.

## 8. Rollback

Revert the 087 commits. Tab 3 returns to its wiring placeholder; no data, schema or configuration
change needs undoing.

## 9. Actual elapsed

Claim `2026-10-15T13:59:11Z` → result ≈ `14:07Z` ⇒ **≈8 minutes**, well inside the 30-minute hard stop.
