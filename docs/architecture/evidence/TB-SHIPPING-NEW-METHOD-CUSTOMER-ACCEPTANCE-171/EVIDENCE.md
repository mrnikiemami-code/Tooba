# TB-SHIPPING-NEW-METHOD-CUSTOMER-ACCEPTANCE-171 — Evidence

Parent: `TB-SHIPPING-NEW-METHOD-ACTIVATION-170`
Base SHA: `325315d626f6d347512e0fb0869aa8c56a5cc707`
Mode: bounded private authenticated canonical-only method acceptance (real HTTP + DB, no design doc)
Verdict: **PASS**

## What was proven

A brand-new canonical flat method (`tipax:scratch171`) with **no legacy `shipping_service_options` row**:

1. is created through the real Admin one-level method endpoint and is inactive with **zero** fabricated
   legacy option rows, rate policies or prices;
2. is activated through the supported Fulfillment activation endpoint and reports
   `isActive=true`, `changed=true`, `canonicalOnly=true` while still creating no legacy option row;
3. stays **absent** from the authenticated storefront projection while it has no owned rate
   (activation ≠ financial readiness);
4. becomes visible to a genuine authenticated customer, with its canonical identity
   (`serviceCode=tipax`, `optionCode=scratch171`), the **exact** method-owned Shipping amount
   (`185000 IRR`) and the authoritative `leadDays=3` — and never inherits a sibling/provider amount
   (`205000` / `480000` / `260000` / `180000`);
5. can be selected with the **server-returned** date/window (`2026-10-22`, `9-12`), and the accepted
   draft reads back at `185000` exactly once (no double charge, no order/checkout/payment row);
6. fails closed when its coverage declaration is switched to `Restricted` without an eligible own grant:
   the method disappears from the customer tree, nothing is priced at zero/FREE, and an attempted
   selection returns `shipping.method.unavailable` without storing a zero/legacy amount;
7. an invalid schedule selection (`2020-01-01`) is rejected with `shipping.delivery.too_early` and
   cannot corrupt the previously accepted draft.

Separately, the **gate-OFF control** on the same scratch context (Host relaunched from the same
published binary with `Tooba:ShippingOwnedRateAuthority:Enabled=false`) shows the new canonical-only
method **absent** and the legacy tree preserving its prior committed pricing
(`tipax:express 260000`, `tipax:standard 180000`), with `unavailableMethods` absent.

## Assertion matrix

| Phase | Assertions | Passed |
| --- | --- | --- |
| gate ON (`assertion-matrix-on-171.json`) | 31 | 31 |
| gate OFF control (`assertion-matrix-off-171.json`) | 6 | 6 |

## Files

| File | Contents |
| --- | --- |
| `prepare-isolated-target-171.js` | fail-closed scratch overlay builder (single tenant, SingleStore, gate from env) |
| `bootstrap-scratch-171.ps1` | disposable container + 2 DBs + publish + canonical `MigrationRunner apply --all-tenants` + private Host |
| `relaunch-isolated-host-171.ps1` | relaunch of the same binary for the OFF control |
| `scratch-171-base-fixture.sql` | scratch-only Geography chain + Persian labels + owned rates for the **pre-existing** legacy methods |
| `reset-scratch-171.sql` | removes only the rows the acceptance itself created (repeatable run) |
| `acceptance-171.js` | the real Admin + authenticated customer acceptance (`on` / `off` phases) |
| `assertion-matrix-on-171.json`, `assertion-matrix-off-171.json` | machine assertion matrices |
| `http-trace-on-171.txt`, `http-trace-off-171.txt` | sanitized request/response traces (no credentials/tokens) |
| `projection-on-171.json`, `projection-off-171.json` | sanitized projections (baseline / ready / selected / restricted / off) |
| `db-readback-on-171.txt` | scratch DB readback of method, rate, coverage, draft, checkouts, payments |
| `isolation-guard-on-171.json`, `isolation-guard-off-171.json` | fail-closed isolation guards |
| `host-launch-171-true.txt` | private Host launch with migration exit + health |
| `cleanup-171.txt` | teardown proof (container removed, ports closed) |

## Isolation

- disposable PostgreSQL container `tooba-tb171-pg` on `127.0.0.1:55490`, databases
  `tooba_171_scratch` / `tooba_171_messaging`, removed after the run;
- private Host on `127.0.0.1:5425` only (never `:3000`, `:5088`, `:5432`), SingleStore mode with one
  Active tenant pointing at the scratch reference;
- committed `ShippingOwnedRateAuthority.Enabled` stays `false` in `appsettings.json`,
  `appsettings.Development.json` and `appsettings.Production.json` (verified);
- no PSP/SMS/final-checkout call, no order/payment created (`checkouts=0`, `payments=0`).

## Provenance of the numbers

- `185000 IRR` is the amount the owner Admin API was asked to store for `tipax:scratch171` and is read
  back from `shipping.rate_policies` (`db-readback-on-171.txt`) — it is not derived from the response.
- `leadDays=3` comes from the same owner row (`lead_days=3`) and is carried into the projection/horizon.
- The legacy `205000/480000` (gate ON) and `260000/180000` (gate OFF) values come from the fixture and
  from committed `ShippingMethods:Rates` respectively; neither is ever used for the new method.

## Scope notes / limitations

- No production code change was required: the TB-170 activation + owned-rate seams already expose the
  canonical-only method, and this task proves the customer path over real HTTP. The only changes in
  this task are evidence/harness files under `docs/architecture/evidence/TB-SHIPPING-NEW-METHOD-CUSTOMER-ACCEPTANCE-171/`.
- No browser DOM/screenshot leg was run; the mandatory HTTP+DB criteria were prioritised inside the
  timebox, so this report claims no browser proof.
- The owned rate was declared at country+administrative-area scope because the storefront selection
  path resolves the address by province/city **names** and stops at administrative-area level in this
  chain. This is a strictly wider owner scope for the same destination, not a fabricated price.
- Payment draft / final checkout were intentionally not exercised; the assertion instead proves that
  no checkout or payment row exists after selection.

## Cleanup

`cleanup-171.txt`: Host processes stopped, `tooba-tb171-pg` removed, ports `5425/55490/3120` closed,
shared ports untouched.
