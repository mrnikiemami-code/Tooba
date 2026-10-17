# TB-SHIPPING-ADMIN-FLAT-METHOD-CREATE-ACCEPTANCE-168 — Evidence

Closes the exact runtime-acceptance gap declared by `TB-SHIPPING-ADMIN-FLAT-METHOD-CREATE-167`
(“no live authorized HTTP create, no real GET read-back, no browser reopen, no duplicate/authorization
proof against a real PostgreSQL”). No production code was changed by this task: it is a runtime
acceptance slice over the already-implemented canonical create contract.

## Task-owned isolated environment (never shared)

| Resource | Value | Notes |
| --- | --- | --- |
| PostgreSQL container | `tooba-tb168-pg` on `127.0.0.1:55487` | disposable, removed at teardown |
| Databases | `tooba_168_scratch`, `tooba_168_messaging` | created for this task only |
| Host | `http://127.0.0.1:5422` | `Tooba.Host.exe`, Development, `SingleStore` |
| Tenant mapping | `store-alpha -> tenant-168-scratch` | single active tenant |
| Next frontend | `http://127.0.0.1:3117` | `TOOBA_HOST_ORIGIN=http://127.0.0.1:5422` |
| One-level catalog gate | `Tooba:ShippingOneLevelCatalog:Enabled=true` | overlay file only |
| Authorization mode | `InMemory` | no shared identity store |
| Rate authority | untouched (`shippingRateAuthoritySet=false`) | no rate policy written |

The task did not open, restart, migrate or write to the shared `:3000` / `:5088` / `:5432`
(`tooba_alpha`, `store-alpha`). `isolation-guard.json` records the machine-checked assertions:
`containsSharedAlphaDatabase=false`, `containsSharedAlphaReference=false`,
`containsSharedAlphaPort=false`, `containsSharedPriorScratchPort=false`.

## 1. Authorized HTTP acceptance — 30/30 PASS

`acceptance-168.js` → `assertion-matrix-http-168.json` (`total=30, passed=30, failed=0`), raw
request/response trace in `http-trace-168.txt`.

| # | Check | Result |
| --- | --- | --- |
| 1 | `dev_context_available` | PASS — 200, actor resolved |
| 2 | `one_level_list_anonymous_fail_closed` | PASS — 401 `admin.actor.missing` |
| 3 | `one_level_list_unknown_actor_fail_closed` | PASS — 403 `admin.authorization.denied` |
| 4 | `access_control_bootstrap_ok` | PASS — 204 |
| 5 | `fulfillment_manage_granted` | PASS — 200, 55 grants |
| 6 | `real_language_ids_read` | PASS — `fa-IR`, `en-US` from the language registry |
| 7 | `baseline_seven_methods_present` | PASS |
| 8 | `binding_unresolved_before_create` | PASS — 200, `resolved=false` |
| 9 | `canonical_create_2xx` | PASS — `tipax:sameday` created |
| 10 | `created_identity_is_method_code` | PASS |
| 11 | `created_is_inactive` | PASS — `isActive=false` |
| 12 | `smuggled_binding_fields_ignored` | PASS — `serviceCode`/`optionCode`/`isActive` in the body have no effect |
| 13 | `readback_exactly_one_new_row` | PASS — authoritative GET |
| 14 | `readback_inactive_canonical` | PASS |
| 15 | `readback_localized_fa_name` | PASS — `تیپاکس همان‌روز` |
| 16 | `readback_localized_en_name` | PASS — `Tipax Same Day` |
| 17 | `server_derived_binding_post_option` | PASS — `tipax:sameday` resolves to `(tipax, sameday)` |
| 18-20 | `existing_binding_intact_tipax_standard` / `tipax_express` / `post_standard` | PASS — pre-existing bindings untouched |
| 21 | `existing_codes_still_present` | PASS |
| 22 | `duplicate_code_rejected` | PASS — HTTP 400 |
| 23 | `duplicate_stable_error_code` | PASS — `shipping_method.code.duplicate` |
| 24 | `unknown_language_rejected` | PASS — `shipping_service.language_invalid` |
| 25 | `malformed_code_rejected` | PASS — `shipping_method.code.invalid` |
| 26 | `parallel_duplicate_creates_single_winner` | PASS — two concurrent POSTs, exactly one row |
| 27 | `no_rows_from_rejected_requests` | PASS — rejected requests persisted nothing |
| 28 | `anonymous_create_rejected` | PASS — 401 |
| 29 | `ungranted_create_rejected` | PASS — 403 |
| 30 | `unauthorized_requests_persisted_nothing` | PASS |

## 2. Real browser create / reopen — `ui-general-tab-browser-created-168.png`

Private browser against the isolated frontend `http://127.0.0.1:3117`:

1. opened `/fa/admin/shipping-services` (flat list rendered from the isolated Host);
2. opened the Add Service modal and submitted the canonical code `tipax:ondemand` with
   Persian (`تیپاکس درجا`) and English (`Tipax On-Demand`) names;
3. the grid refreshed **from the server** with no page reload and showed the new row with the
   inactive state;
4. the row was reopened: the four non-nested detail tabs are present and the General tab shows the
   immutable code plus both saved translations, with status inactive (screenshot);
5. a full document reload kept the row (server-persisted, not optimistic state).

Corroborating authoritative SQL readback in `db-readback-final-168.txt`:

```
 method_code    | legacy_service_code | legacy_option_code | is_active | provider_kind
 tipax:ondemand | tipax               | ondemand           | f         | tipax
```

with `tipax:ondemand | en-US | Tipax On-Demand` and `tipax:ondemand | fa-IR | تیپاکس درجا`.

## 3. Server-derived binding, inactive default, no fabricated policy

`db-readback-final-168.txt` over all four created methods (`tipax:sameday`, `post:smuggled`,
`mahex:parallel`, `tipax:ondemand`):

- every created row has `is_active = f` (the create contract has no client-settable status field);
- `legacy_service_code` / `legacy_option_code` are exactly the server-derived split of the canonical
  code (`service:option ⇒ (service, option)`), never client-supplied;
- `new_legacy_services = 0`, `new_legacy_options = 0`, `rate_policies = 0`, `coverage_grants = 0`,
  `method_coverages = 0` for the new methods.

## 4. Customer non-selectability

- Authoritative state: all four created methods are `is_active = false`.
- Authoritative read path: storefront eligibility is built from
  `StorefrontShippingService.LoadEligibleMethodsAsync` —
  `catalog.Where(x => x.IsActive)` (line 672) and `service.Options.Where(o => o.IsActive)`
  (line 687) — so an inactive method can never reach `Methods` in the storefront projection or the
  checkout selection.
- Activation is a separate explicit admin surface (`.../one-level/{code}/activation`) and was never
  called by this task: the create contract only ever writes inactive rows.

`DISCLOSED`: this slice proves non-selectability from the authoritative persistent state plus the
storefront eligibility filter; it did **not** run a live `POST /v1/storefront/shipping/projection`
for the created methods, because the scratch tenant has no seeded catalog/offers and therefore no
live cart to project. The claim is “cannot be selected”, not “observed absent in a live projection
response”.

## 5. Runtime + teardown

`runtime-snapshot-168.txt` (pre-teardown): isolated Host `200`, isolated frontend `/fa/admin/
shipping-services` `200` (49,176 bytes), admin list without identity `401`, shared `:5088` `200`,
shared `:3000` `200`.

`cleanup-168.ps1` → `cleanup-168.txt`: stops the isolated Host/frontend, removes the disposable
container `tooba-tb168-pg`, deletes `.tmp-168-run`, then verifies
`scratch_ports_closed=True` (5422, 3117, 55487) and `shared_ports_intact=True` (3000, 5088, 5432).

## Files

- `prepare-isolated-target-168.js`, `launch-isolated-host-168.ps1`, `launch-isolated-frontend-168.ps1`
- `acceptance-168.js`, `assertion-matrix-http-168.json`, `http-trace-168.txt`
- `catalog-final-168.json`, `db-readback-final-168.txt`, `effective-config-168.json`
- `ui-general-tab-browser-created-168.png`
- `runtime-snapshot-168.txt`, `cleanup-168.ps1`, `cleanup-168.txt`
