# SHIPPING-ONE-LEVEL-UI-089 — tab 5 «بررسی هزینه»

Task: `TB-SHIPPING-ONE-LEVEL-TABS-089` (parent `TB-SHIPPING-ONE-LEVEL-TABS-088`)
Mode: `BOUNDED_ONE_LEVEL_OPERATIONAL_QUOTE_INSPECTOR_TAB`
Stop-State: `STOP_FOR_ARCHITECT`
Expected-Base-SHA: `4dc0b1dacaeffbc6e5ac6e9113dfd2040f0919d5`

## 1. Scope actually delivered

Tab 5 — the fifth and last tab of the default-OFF one-level shell — is now a real **read-only
diagnostic** cost inspector for exactly one selected one-level method. It reuses the existing
authorized 076/078 operational-quote seam and the proven quote model unchanged. **No backend source
changed**: the 086 binding resolver already provides the immutable legacy identity, and the existing
`POST /v1/admin/shipping-services/{serviceCode}/operational-quote` route is the only route called.

It adds no second level, no price authority, no write of any kind.

## 2. Endpoint contract confirmation (source-checked before wiring)

Confirmed from backend source, not assumed:

* Route: `group.MapPost("/shipping-services/{serviceCode}/operational-quote", EvaluateAsync)` in
  `Tooba.Shipping.Endpoints/Admin/ShippingOperationalQuoteEndpoints.cs`.
* Authorization: exactly one mapped route and one `RequireAuthorizedAsync` call with
  `ShippingAdminPermissions.View` (never `Manage`, never `AllowAnonymous`), asserted by the existing
  `ShippingOperationalQuoteEndpointOwnershipTests`.
* The 078 client sends only declared DTO fields (`scope`, `cartSubtotalExclusive`, `currencyCode`,
  `optionCode`, `weightGrams`, `itemCount`, `requestedPickupAtUtc`), so no invented wire field exists.
* No new endpoint was invented and the `operational-quote`/`quote-inspector` path exception was **not**
  used: the existing client and model were reused as-is without modification.

## 3. Mission A — real fifth tab, no second level

| Rule | How it is enforced |
| --- | --- |
| Method code + label shown, never split | The tab renders `bindingContextFa(resolvedBinding)` and calls only `oneLevelBindingPath(methodCode)`; no `split(":")` (asserted by test). |
| Binding exclusively from the server | `oneLevelQuotePayload` **overwrites** `form.optionCode` with `binding.optionCode`, and the request service code comes only from `quoteTarget(resolvedBinding)`. A client-supplied option can never reach the wire. |
| `null` for optionless methods | `store_courier`/`in_person` send `optionCode: null`, asserted. |
| No provider level / option selector / grouping | No option `<select>`, no `SERVICE_LEVEL_OPTION_VALUE`, no `loadShippingMethodTree`, no `providerKind` (asserted). |
| Fail closed | `resolveQuoteState` returns `binding-unavailable` for `gate-off`, `not-ready`, `unresolved`, `untrusted`, and `no-method` without a selection; no price, no zero default and no success is manufactured. |
| Reply identity checked | `quoteMatchesRequestedIdentity` refuses a reply whose `serviceCode` **or** `optionCode` differs from the requested identity — the amount is not displayed at all. |
| Stale selection | The generation guard bumps on every method change, form/result/failure are reset, and `operationStillTargetsMethod` keeps a late reply from being attributed to the new method. No cancellation of a committed server call is claimed, and the submit button is disabled while busy (no duplicate submit). |
| Input semantics reused | Geographic identity via the existing Geography-only picker; explicit IANA timezone; local date/time resolved through the existing `resolvePickupToUtc` (including its ambiguous/nonexistent refusal); subtotal, weight grams and physical item count through the existing parsers; currency through the existing normalization. Weight/count are labeled as **manual diagnostic inputs**, never as measured order facts. |
| Display is faithful, not recomputed | Every amount is rendered with `formatAmountFa` from the backend answer; a nullable amount stays «اعلام نشده» and is never shown as `0`. Nothing is recomputed into checkout pricing. |
| Currency mismatch visible | A reply currency differing from the request is flagged, not normalized away. |

### Honesty statements carried in the UI

* Persistent: **«فقط بررسی هزینه؛ مبلغ سفارش یا پرداخت تغییر نمی‌کند.»**
* Diagnostic boundary: a read-only inspection, not a customer price, not a transaction; no order is
  written, no payment or collect-on-delivery collection is created, and Checkout keeps its old price
  authority.
* Amount boundary: «قابل پرداخت هنگام تسویه» and «وصول هنگام تحویل» are diagnostic split of the same
  evaluation — neither an amount taken from the customer nor an amount collected or settled.
* Capacity: **«ظرفیت تنظیم‌شده (هنوز اعمال نمی‌شود)»**; the runtime enforcement remains `NotEnforced`.
* `NotEvaluated` availability never claims «قابل رزرو».
* Carrier windows are pickup/dispatch availability, not a promised customer delivery.
* Componentized result: carrier freight, packaging, combined service total, checkout-due, COD-due,
  payer mode, availability verdict, matched size tier, eligibility and the actual freight/packaging
  reason codes returned by the API.

### UI states

no method; binding loading; binding unavailable (gate OFF / not migrated / unresolved / invalid);
401/403 denied; invalid input; pending evaluation; valid result; business refusal or failure code;
unexpected API failure; stale/identity mismatch. Persian RTL, the 084 tablist/tab/tabpanel keyboard
semantics and tabs 1–4 are unchanged. The legacy inspector still works while the gate is OFF.

**This tab writes nothing**: evaluation is read-only semantics notwithstanding the existing POST verb.

## 4. Changed paths

Frontend only:
* `src/frontend/app/admin/shipping-services/shipping-one-level-quote-model.ts` (new)
* `src/frontend/app/admin/shipping-services/shipping-one-level-quote-tab.tsx` (new)
* `src/frontend/app/admin/shipping-services/shipping-one-level-quote-tab.test.ts` (new)
* `src/frontend/app/admin/shipping-services/shipping-one-level-tabs.tsx` (tab-5 wiring)
* `src/frontend/app/admin/shipping-services/shipping-one-level-tabs-model.ts` (wiring map now empty)
* `src/frontend/app/admin/shipping-services/shipping-one-level-tabs.test.ts` (all five tabs wired)

Docs:
* `docs/planning/shipping/SHIPPING-ONE-LEVEL-UI-089.md`
* `docs/architecture/evidence/TB-SHIPPING-ONE-LEVEL-TABS-089/**`
* `docs/ai/tasks/TB-SHIPPING-ONE-LEVEL-TABS-089.task.md` (receipt, kept UNSTAGED)

## 5. Tests and results

| Command | Result |
| --- | --- |
| `node --test --experimental-strip-types …shipping-one-level-quote-tab.test.ts` + `…-tabs.test.ts` | 25 passed / 0 failed |
| `node --test --experimental-strip-types "app/admin/shipping-services/*.test.ts"` | 173 passed / 0 failed |
| `npm run typecheck` (frontend) | clean |

One intermediate failure was found and fixed honestly during development: a boundary-text assertion
rejected the words «وصول/تسویه» even in their negating form; the assertion now targets positive claims
only.

Tests not run and why: no backend suite (no backend source changed); no Host test build against the
locked output DLLs; no broad suite.

Focused coverage added: server-owned identity binding (`post:standard`, `tipax:express`, optionless
`store_courier`, inactive/unmapped), fail-closed binding states, no `methodCode` splitting, no option
picker, stale async reply, strict target isolation, reply-identity mismatch refusal, optionless `null`
propagation, the diagnostic/no-charge/no-settlement boundary text, display-only amount handling, and
tab 1–4 preservation plus the locked five Persian labels.

## 6. Gate / database truth — NOT LIVE VERIFIED, NOT COMMERCIALLY READY

* Gate `Tooba:ShippingOneLevelCatalog`: **OFF by absence**; not switched by this task.
* Development one-level table/backfill: **absent/unapplied**; the running Host may predate the 086 route.
* `ShippingRateAuthority`: **OFF**. `ShippingOperationalShadow`: **OFF**.
* Therefore tab 5 is **source-and-focused-test proof only**. No live tab-5 evaluation is claimed and no
  business acceptance is claimed.
* No database write at all: no DDL/DML, no migration/backfill, no seeding, no gate activation, no new
  permission, no Host/Next restart, no staging/production contact, no unrelated file touched.

## 7. Blockers

1. Live tab-5 evaluation needs the gate enabled **and** the authorized one-level migration — a separate
   task (Architect must decide 090 scope for a controlled Development migration/backfill and real UI
   verification).
2. The 086 Host cert-guard run remains blocked while `Tooba.Host` (pid 33548) holds its output DLLs.
3. The legacy operational-quote dialog remains the operational inspector while the gate is OFF.

## 8. Rollback

Revert the 089 code commit and its evidence commit. Tab 5 returns to its wiring placeholder; no data,
schema or configuration change needs undoing.

## 9. Actual elapsed

Claim `2026-10-15T14:10:44Z` → result ≈ `14:19Z` ⇒ **≈9 minutes**, inside the 30-minute hard stop.
