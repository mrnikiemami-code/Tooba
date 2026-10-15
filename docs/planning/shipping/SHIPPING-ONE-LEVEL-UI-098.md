# SHIPPING-ONE-LEVEL-UI-098 — shared Development Host restore + real read-only browser proof

Task: `TB-SHIPPING-ONE-LEVEL-TABS-098` (parent `TB-SHIPPING-ONE-LEVEL-TABS-097`)
Mode: `BOUNDED_SHARED_DEVELOPMENT_ACTIVATION_UI_READ_ONLY_PROOF`
Stop-State: `STOP_FOR_ARCHITECT`
Evidence: `docs/architecture/evidence/TB-SHIPPING-ONE-LEVEL-TABS-098/**`

## 1. Why this task existed

097 shipped the option-backed activation UI and passed as **source/test proof**, but its
shared-Development evidence could not show the control live:

* the running shared Host on `127.0.0.1:5088` had been built at 22:27 (before the 096 commit at
  01:40), so it did **not** contain the 096 `PUT .../one-level/{methodCode}/activation` route;
* the shared Next app therefore never rendered the one-level shell in that session.

098 restores an operator-safe, reproducible **current-code** Development stack and establishes
real browser READ-ONLY proof of the 097 control.

## 2. Baseline (no drift)

```
Expected-Base-SHA = d6ba104368c974094ff4ab2cd667bcb8ea92bf83
HEAD              = d6ba104368c974094ff4ab2cd667bcb8ea92bf83   (== origin/main)
src drift in d6ba1043..HEAD = none
```

## 3. Controlled deployment (existing repo-owned script only)

Command actually used (nothing else):

```powershell
powershell -NoProfile -File scripts/dev/host-stack.ps1 `
  -Action restart `
  -OutDir .tmp-dev-host-out-5088-098 `
  -NoBuild `
  -GateShippingOneLevelCatalog `
  -TakeOverProvenDevelopmentHost
```

Pre-steps that made it safe:

1. `dotnet build src/backend/Host/Tooba.Host/Tooba.Host.csproj -c Debug -o .tmp-dev-host-out-5088-098`
   => `0 Error(s)`, 19 warnings, 32.8 s. Isolated, task-owned output directory (the shared
   `.tmp-dev-host-out-5088` was locked by the running process).
2. The built `Tooba.Fulfillment.Endpoints.dll` was checked for the 096 route literal
   `shipping-methods/one-level/{methodCode}/activation` => present.
3. Port ownership was proven **before** touching anything:
   * `:5088` pid 30756 `Tooba.Host.exe`, exe under this repo (`.tmp-dev-host-out-5088`),
     matching the script's own PID file `.tmp-dev-host/host-5088.pid`
     (recorded gate `ShippingOneLevelCatalog=true`) — i.e. already script-managed;
   * `:3000` pid 31580 `node.exe` (Next dev server) — left untouched.
   Therefore the script's own `Stop-ManagedHost` ownership proof applied (graceful
   `Stop-Process`, no `-Force`).

Result: `:5088` is now pid 29788 from `.tmp-dev-host-out-5088-098`, `/health` ok,
provenance `SingleStore / dev-singlestore / store-alpha / tenant-alpha`.
The gate was passed **only** as an ephemeral process environment variable
(`Tooba__ShippingOneLevelCatalog__Enabled=true`) — never written to `appsettings` or source.
`ShippingRateAuthority` and `ShippingOperationalShadow` are never set by the script (remain OFF).
The Next dev server on `:3000` was not restarted or disturbed.

## 4. What the read-only proof established

| Item | Result |
| --- | --- |
| Five Persian RTL tabs | ✔ `سرویس‌ها / تعرفه و پوشش / بسته‌بندی و کرایه / برنامه کاری و ظرفیت / بررسی هزینه` |
| Flat seven-method list | ✔ 7 rows, one per canonical method |
| 094 name-editor action | ✔ 7 × `ویرایش نام`; dialog opens pre-filled from live translations; cancelled, never saved |
| 097 activation control (option-backed) | ✔ `post:standard` offers `غیرفعال‌سازی` with explicit impact + confirm dialog (cancelled) |
| 097 activation control (inactive) | ✔ `post:express` offers `فعال‌سازی` with explicit impact + confirm dialog (cancelled) |
| Eligibility source | ✔ server-owned 086 binding (`post · standard`, `post · express`); no client-side code splitting |
| Optionless refusals | ✔ `store_courier`, `in_person`, `snapp_courier` show «فعال‌سازی این نوع سرویس هنوز پشتیبانی نمی‌شود.» and expose **no** toggle |
| No write | ✔ zero non-GET requests recorded; no PUT; no DML |
| No fake success / no optimistic state | ✔ no alert rendered; displayed state stayed equal to server truth after each cancel |
| Route mounting (current code) | ✔ `GET`/`OPTIONS` on `.../activation` => `405 Allow: PUT` (was `404` on the pre-096 build) |
| Server truth after the run | ✔ gate Enabled, ready true, 7 methods, 14 translations, inactive = `post:express, snapp_courier` |
| DB parity pre/post | ✔ identical (see §5) |

## 5. DB parity (read-only SELECT only, container `postgres-db`, db `tooba_alpha`)

```
legacy_services=5              one_level_methods=7        inactive_methods=post:express,snapp_courier
legacy_options=4               one_level_translations=14  active_methods=in_person,post:standard,store_courier,tipax:express,tipax:standard
legacy_service_translations=10 inactive_options=express
legacy_option_translations=8   method_codes=in_person,post:express,post:standard,snapp_courier,store_courier,tipax:express,tipax:standard
option_natural_hash=4fd8872b79e0141c44385b6f44b9f691   (identical pre and post)
seller_orders=26   order_lines=37   fulfillments=9
```

`seller_orders=26 / order_lines=37 / fulfillments=9` match the historical parity recorded by
`TB-SHIPPING-ONE-LEVEL-TABS-093` (`shipping_service_options=4; order.seller_orders=26;
order.order_lines=37; fulfillment.fulfillments=9`) => historical Order/Fulfillment counts unchanged.

No activation update, no rate update, no policy/schedule write, no migration, no seed, no COD and
no Checkout/Payment/Settlement change was made.

## 6. Rollback (reproducible, gate-OFF)

The previous shared state was *also* a gate-ON Development Host, so leaving the new Host up
preserves the operator's prior configuration. To return the shared Host to **gate-OFF** defaults:

```powershell
powershell -NoProfile -File scripts/dev/host-stack.ps1 -Action stop
powershell -NoProfile -File scripts/dev/host-stack.ps1 -Action start -OutDir .tmp-dev-host-out-5088-098
```

`-Action start` without `-GateShippingOneLevelCatalog` never sets the variable, so the gate falls
back to its disabled default and the one-level shell disappears from the admin page (legacy screen
returns). To fully return to the pre-098 build directory:

```powershell
powershell -NoProfile -File scripts/dev/host-stack.ps1 -Action stop
powershell -NoProfile -File scripts/dev/host-stack.ps1 -Action start -OutDir .tmp-dev-host-out-5088 -NoBuild
```

The 097/096 code is unchanged; nothing was persisted in configuration or source.

## 7. Limitations (not masked)

* No live `PUT` to the activation endpoint was executed, so the **successful write branch**
  (server confirmation + catalog/binding re-read) is **NOT VERIFIED live** by 098. It remains
  covered only by the historical 096 backend tests (154/154) and 097 frontend tests (204/204),
  reported as historical evidence, not re-executed here.
* The authorized actor in the browser is the existing authenticated Development administrator
  session. Anonymous access is `401` and a non-member dev actor is `403`.
* One method-correct `PUT` probe was sent to the **pre-deploy** activation path as a route check;
  it returned `404` (route did not exist), reached no handler and produced no DB change (identical
  pre/post parity confirms this). No PUT was sent to the post-deploy Host.
