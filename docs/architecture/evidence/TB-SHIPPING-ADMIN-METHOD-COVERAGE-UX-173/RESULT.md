# TB-SHIPPING-ADMIN-METHOD-COVERAGE-UX-173 — result

- Task-ID: TB-SHIPPING-ADMIN-METHOD-COVERAGE-UX-173
- Parent-Task: TB-SHIPPING-NEW-METHOD-BROWSER-PAYMENT-172
- Channel: tooba-main
- Base-SHA: 1f5edc29db4a5ba0a7ac523e6b920981186a7103
- Verdict: PASS (server-truth + fail-closed + focused tests). Browser click-through not executed in this run — see gaps.

## What changed (task-scoped only)

| Path | Change |
| --- | --- |
| `src/frontend/app/admin/shipping-services/shipping-rates-coverage-api.ts` | Maps `methodCoverages` (unknown mode ⇒ dropped + `methodCoveragesUntrusted`), keeps `optionCode` on grants and grant writes, adds `setAdminShippingMethodCoverage` on the existing `PUT …/method-coverage`. |
| `src/frontend/app/admin/shipping-services/shipping-one-level-rates-model.ts` | `resolveCoverageMode` (identity = service + option; absent = Unrestricted default; wrong service/unreadable = untrusted), mode text/boundary/audit constants, write-eligibility and empty-restricted guards, and option-exact `grantBelongsToBinding`. |
| `src/frontend/app/admin/shipping-services/shipping-method-coverage-mode-section.tsx` (new) | Compact RTL section: current server mode, boundary note, Unrestricted/Restricted radio, existing geography picker, declaration + option-scoped grant save, server re-read, actionable errors, read-only state. |
| `src/frontend/app/admin/shipping-services/shipping-one-level-rates-tab.tsx` | Hosts the section, propagates `readOnly`, and consolidates the old service-level grant button into the method-scoped section (a grant can no longer be written without the method's own option). |
| `src/frontend/app/admin/shipping-services/flat-method-detail-dialog.tsx` | Passes `readOnly` (`mode === "view"`) into the rates tab. |
| `src/frontend/app/admin/shipping-services/shipping-method-coverage-mode.test.ts` (new), `shipping-one-level-rates-tab.test.ts`, `shipping-rates-coverage-model.test.ts` | Focused guards for default/declared/untrusted mode, sibling isolation, grant identity, wire shape, denied/invalid responses, read-only, and source wiring. |
| `docs/ai/tasks/TB-SHIPPING-ADMIN-METHOD-COVERAGE-UX-173.task.md` | Persisted task artifact. |
| `docs/architecture/evidence/TB-SHIPPING-ADMIN-METHOD-COVERAGE-UX-173/` | Isolation scripts, acceptance harness, assertion matrix, HTTP trace, teardown proof. |

No backend change was needed: the Shipping owner already exposed the declaration endpoint, the
option-scoped grant writer and `methodCoverages` in the read model. No new API, schema, migration,
financial mode, flag, Checkout/Payment/Fulfillment/Settlement change, or seller/carrier promise.

## Verification

1. `npm run typecheck` (src/frontend) — PASS.
2. Focused frontend tests — 49 PASS (new suite 11/11) covering:
   default Unrestricted text without coverage promise; per-option declaration read; wrong-service /
   unreadable ⇒ untrusted + no write; read-only/in-flight ⇒ no write; Restricted-empty requires a
   destination; no sibling/service-level grant inheritance; exact `PUT` body; invalid/denied response
   never success; grant wire carries the method option; declaration mapping + untrusted flag.
3. `dotnet test Tooba.Shipping.Tests --filter "ShippingMethodCoverageRestrictionTests|ShippingRateCoverageAdminTests|ShippingAdminEndpointOwnershipTests|ShippingStrictOwnedOptionRateTests"`
   — PASS 30/30 (owner declaration persistence, option-scoped grant identity, strict owned-rate
   quote fail-closed behavior, endpoint ownership).
4. Isolated owner-API acceptance (disposable PostgreSQL `tooba-tb173-pg` on 127.0.0.1:55493 +
   private Host on 127.0.0.1:5427, gate `ShippingOwnedRateAuthority` only in the scratch overlay):
   20/20 PASS — `assertion-matrix-173.json`, `http-trace-173.txt`.
   - default read: no declaration row for `tipax:standard`, none for sibling `tipax:express`;
   - `PUT …/method-coverage {optionCode:"standard",mode:"Restricted"}` ⇒ 200, persisted, stable id on re-save;
   - `POST …/coverage-grants {serviceCode:"tipax",optionCode:"standard",scope:{countryId}}` ⇒ 200;
   - reload ⇒ same Restricted declaration + same active destination, exactly one declaration row;
   - sibling `tipax:express` stays undeclared with zero grants; DB counts match identity;
   - switch back to `Unrestricted` ⇒ reload shows Unrestricted and the grant row is retained (auditable);
   - anonymous write ⇒ 401 (`admin.actor.missing`) and no state change.
5. Architecture guards: import-boundary and flat-folder PASS. Source-size guard fails only on four
   files that are identical to `HEAD` and already above their baseline (pre-existing drift; baseline
   repair explicitly out of scope). Touched files: rates tab 790 LOC (limit 800), new section 260.

## Isolation / cleanup

- Only 127.0.0.1:55493 (PostgreSQL), 127.0.0.1:5427 (Host), 127.0.0.1:3122 (reserved, unused).
- Shared `:3000`, `:5088`, `:5432`, `tooba_alpha`, `store-alpha` and shared RabbitMQ/Redis never touched.
- Committed `appsettings*` authority flags remain `false`; overlay was disposable.
- `cleanup-173.txt`: container removed, no process or port left listening.

## Outstanding gaps (for Architect)

- The Admin **browser** leg (open the dialog in Edit, click the radios, pick a destination, save,
  close/reopen, and the View-mode read-only control) was not executed; proof for that surface is
  typecheck + focused tests + source guards, not a live browser. No screenshot is claimed.
- Strict customer quote with a method-owned rate and an allowed/denied destination was proven by the
  existing focused backend tests, not re-proven end-to-end through a storefront cart in this run.
