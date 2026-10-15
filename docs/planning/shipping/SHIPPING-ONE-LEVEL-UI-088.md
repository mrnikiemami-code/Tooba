# SHIPPING-ONE-LEVEL-UI-088 — tab 4 «برنامه کاری و ظرفیت»

Task: `TB-SHIPPING-ONE-LEVEL-TABS-088` (parent `TB-SHIPPING-ONE-LEVEL-TABS-087`)
Mode: `BOUNDED_ONE_LEVEL_OPERATING_SCHEDULE_AND_CAPACITY_TAB`
Stop-State: `STOP_FOR_ARCHITECT`
Expected-Base-SHA: `f87b036852da34b5b70055a8ed9c38db42dea47f`

## 1. Scope actually delivered

Tab 4 of the default-OFF one-level shell is now a real weekly operating-schedule editor for
**exactly one** selected one-level method. It reuses the existing Shipping-owned operating-schedule
APIs (071/072) and the proven schedule model unchanged. **No backend source changed**: the 086
binding resolver already provides the immutable legacy identity tab 4 needs.

It does not add a second merchant configuration level, does not infer the legacy binding by splitting
`methodCode`, does not enable any gate, and does not touch Order/Checkout/Payment/Settlement.

## 2. Mission A — real tab 4 in the one-level shell

The method→binding authority is the 086 resolver, reused as-is:

```
GET /v1/admin/shipping-services/shipping-methods/one-level/{methodCode}/binding
  -> { gate, ready, resolved, methodCode, serviceCode, optionCode, isActive }
```

| Rule | How it is enforced |
| --- | --- |
| Selected method shown, no split | The tab renders `bindingContextFa(resolvedBinding)` and only calls `oneLevelBindingPath(methodCode)`; no `split(":")` anywhere (asserted by test). |
| Binding exclusively from the server | The PUT target is produced only by `scheduleWriteTarget(resolvedBinding)`; there is no editable service/option input. |
| Fail closed on gate OFF / unavailable / absent / invalid / denied | `resolveScheduleState` returns `binding-unavailable` for `gate-off`, `not-ready`, `unresolved`, `untrusted`; `denied` for 401/403; the component renders an explicit refusal with no list and no editor. |
| No option selector / provider sublevel / grouping | No option `<select>`, no `scheduleOptionLabelFa`, no `SERVICE_LEVEL_SCHEDULE_VALUE`, no `loadShippingMethodTree` (asserted by test). |
| Only the selected identity is read or written | `scopeSchedulesToBinding` matches `serviceCode` **and** `optionCode` exactly (null matches null). A service-level schedule never appears under an option-bearing method and vice-versa. |
| Mismatched responses rejected | `scheduleViewMatchesBindingService` gates rendering and editing, so a schedule of another service can never be shown or mutated from this method's context. |
| Stale selection | `createBindingGenerationGuard` bumps on every method change; `operationStillTargetsMethod` keeps a completion notice tied to the identity that started the operation, and the post-write refresh is skipped when the method changed. No cancellation of a committed server write is claimed. |
| Reused validation/model | ISO weekday identity, 24-hour `HH:mm` parsing, the half-open `[start,end)` rule, the `HH:mm:ss` wire shape, the explicit IANA timezone requirement, capacity parsing and all Persian refusal messages come from `shipping-operating-schedule-model.ts`. |
| Availability semantics untouched | No change to timezone conversion, schedule precedence, or method activation. Inactive schedules are shown truthfully with an explicit count. |

### Capacity and window truth

* Persistent Persian label on every capacity surface: **«ظرفیت تنظیم‌شده (هنوز اعمال نمی‌شود)»**.
* The only runtime enforcement is stated as `NotEnforced`; the strings «ظرفیت باقی‌مانده» and
  «رزرو شد» appear nowhere (asserted by test).
* Carrier windows are described as pickup/dispatch availability, explicitly **not** a promised
  customer delivery (the promise is Order-owned).
* No atomic slot reservation, no capacity claim/release, no delivery promise, no derived parcel
  weight or item count, no COD ledger, no settlement, no Checkout authority.

## 3. Changed paths

Frontend only:
* `src/frontend/app/admin/shipping-services/shipping-one-level-schedule-model.ts` (new)
* `src/frontend/app/admin/shipping-services/shipping-one-level-schedule-tab.tsx` (new)
* `src/frontend/app/admin/shipping-services/shipping-one-level-schedule-tab.test.ts` (new)
* `src/frontend/app/admin/shipping-services/shipping-one-level-tabs.tsx` (tab-4 wiring)
* `src/frontend/app/admin/shipping-services/shipping-one-level-tabs-model.ts` (tab-4 wiring task → null)
* `src/frontend/app/admin/shipping-services/shipping-one-level-tabs.test.ts` (wiring assertion)

Docs:
* `docs/planning/shipping/SHIPPING-ONE-LEVEL-UI-088.md`
* `docs/architecture/evidence/TB-SHIPPING-ONE-LEVEL-TABS-088/**`
* `docs/ai/tasks/TB-SHIPPING-ONE-LEVEL-TABS-088.task.md` (receipt, kept UNSTAGED)

**No backend source changed.** The existing `GET /operating-schedules`,
`PUT /operating-schedules`, `PATCH /operating-schedules/{id}/active` routes already cover read,
create/replace, and activation, so no new Shipping endpoint was invented and the
`operating-schedule` path exception was not needed.

## 4. Tests and results

| Command | Result |
| --- | --- |
| `node --test --experimental-strip-types …shipping-one-level-schedule-tab.test.ts` + `…-tabs.test.ts` | 28 passed / 0 failed |
| `node --test --experimental-strip-types "app/admin/shipping-services/*.test.ts"` | 165 passed / 0 failed |
| `npm run typecheck` (frontend) | clean |

Skipped tests and why: no backend test was run because no backend source changed; the known Host
output DLL lock (`Tooba.Host` pid 33548) was **not** retried and no service was restarted; no broad
suite was run.

Focused coverage added: `post:standard`, `tipax:express`, optionless `store_courier`/`in_person`,
inactive methods, no guessed code split, gate/not-ready/403 refusal, service identity mismatch, stale
selection, the full state machine (never `ready` without a resolved binding), configured-capacity
`NotEnforced` wording, the dispatch-vs-delivery distinction, the truthful empty/inactive text, and
tab 1–3 preservation (the shell wiring test still asserts the locked Persian labels and the wired
tabs).

## 5. Gate / database truth — NOT LIVE VERIFIED, NOT COMMERCIALLY READY

* Gate `Tooba:ShippingOneLevelCatalog`: **OFF by absence**; not switched by this task.
* Development one-level table/backfill: **absent/unapplied**; the running Host may predate the 086
  route.
* `ShippingRateAuthority` and `ShippingOperationalShadow` remain **OFF**.
* Therefore tab 4 is **source-and-test proof only**. No live browser CRUD is claimed, and this
  deliverable is **not** a claim of commercial readiness.
* No DDL/DML, no migration/backfill, no seeding, no config gate activation, no new permission grant,
  no pricing cutover, no Host/Next restart, no staging/production contact. The legacy screen stays
  usable while the gate is OFF.

## 6. Blockers

1. Live tab-4 CRUD needs the gate enabled **and** the authorized one-level migration — a separate,
   unauthorized task. Until then the tab truthfully reports the binding as unavailable.
2. The 086 Host cert-guard run remains blocked while `Tooba.Host` (pid 33548) holds its output DLLs;
   not re-challenged here.
3. The legacy operating-schedule dialog remains the operational surface while the gate is OFF.

## 7. Rollback

Revert the 088 code commit and its evidence commit. Tab 4 returns to its wiring placeholder; no data,
schema or configuration change needs undoing.

## 8. Actual elapsed

Claim `2026-10-15T14:05:03Z` → result ≈ `14:13Z` ⇒ **≈8 minutes**, well inside the 30-minute hard stop.
