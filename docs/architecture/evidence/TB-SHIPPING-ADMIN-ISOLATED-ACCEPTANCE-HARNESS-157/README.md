# TB-SHIPPING-ADMIN-ISOLATED-ACCEPTANCE-HARNESS-157

A durable, rerunnable **private Admin acceptance harness** for the Shipping flat-method commercial
surfaces. It launches a task-owned disposable Postgres, a task-owned Host built from the current HEAD,
and (optionally) a task-owned Next frontend, then proves the real authorized read/write behaviour of the
Shipping admin seams over authenticated HTTP — never against the shared `:3000` / `:5088` / `:5432`
stack, `tooba_alpha`, or `tenant-alpha`.

## Why this exists

TB-155/TB-156 shipped a method-scoped financial read model and a fail-closed edit gate, but their
evidence could not include a live authorized runtime: the previous scratch recipe (TB-154) had already
been torn down, and no script existed that seeds **admin authorization** in a scratch environment. This
harness closes that gap once, for reuse by later acceptance tasks.

## Ports, names and ownership (all task-owned)

| Layer | Endpoint | Owner |
| --- | --- | --- |
| Postgres 16 | `127.0.0.1:55475`, db `tooba_157_scratch` + `tooba_157_messaging` | container `tooba-tb157-pg` |
| Host | `http://127.0.0.1:5417` (`SingleStore`) | `.tmp-157-host-out/Tooba.Host.exe` |
| Next frontend (optional) | `http://127.0.0.1:3111` (`TOOBA_HOST_ORIGIN=http://127.0.0.1:5417`) | task-owned `next dev` |

`prepare-isolated-target-157.js` writes a fail-closed `isolation-guard.json` (one Active tenant,
no `tooba_alpha`, no `tenant-alpha`, no `Port=5432`, no prior scratch port, no `ShippingRateAuthority`,
no `ShippingOperationalShadow`, no Edition swap, `AllowResetAndSeed=false`). The scratch password is
read from `TOOBA_157_SCRATCH_PASSWORD` and is never written into evidence.

## Run order

```powershell
# 1. disposable scratch cluster (task-owned port/name)
docker run -d --name tooba-tb157-pg -e POSTGRES_USER=scratch157 -e POSTGRES_PASSWORD=<env-only> `
  -e POSTGRES_DB=tooba_157_scratch -p 127.0.0.1:55475:5432 postgres:16-alpine
docker exec -e PGPASSWORD=<env-only> tooba-tb157-pg psql -U scratch157 -d tooba_157_scratch `
  -c "CREATE DATABASE tooba_157_messaging OWNER scratch157;"

# 2. publish + isolate
dotnet publish src\backend\Host\Tooba.Host\Tooba.Host.csproj -c Debug -o .tmp-157-host-out
$env:TOOBA_157_SCRATCH_PASSWORD='<env-only>'
node docs\architecture\evidence\TB-SHIPPING-ADMIN-ISOLATED-ACCEPTANCE-HARNESS-157\prepare-isolated-target-157.js

# 3. private Host
powershell -NoProfile -File docs\architecture\evidence\TB-SHIPPING-ADMIN-ISOLATED-ACCEPTANCE-HARNESS-157\launch-isolated-host-157.ps1 -Port 5417

# 4. SCRATCH-ONLY schema + destination fixture (see "Scratch fixtures" below)
powershell -NoProfile -File docs\architecture\evidence\TB-SHIPPING-ADMIN-ISOLATED-ACCEPTANCE-HARNESS-157\seed-scratch-157.ps1

# 5. authenticated acceptance over the private Host only
$env:TOOBA_157_HOST='http://127.0.0.1:5417'
node docs\architecture\evidence\TB-SHIPPING-ADMIN-ISOLATED-ACCEPTANCE-HARNESS-157\acceptance-157.js

# 6. optional private frontend (real Admin browser leg)
powershell -NoProfile -File docs\architecture\evidence\TB-SHIPPING-ADMIN-ISOLATED-ACCEPTANCE-HARNESS-157\launch-isolated-frontend-157.ps1

# 7. ALWAYS cleanup (stops only task-owned PIDs by executable path)
powershell -NoProfile -File docs\architecture\evidence\TB-SHIPPING-ADMIN-ISOLATED-ACCEPTANCE-HARNESS-157\cleanup-157.ps1
```

`acceptance-157.js` expects a **fresh** scratch shipping state; re-running it against an already-written
scratch reports the second write as the denied duplicate (`shipping.rate_policy.scope_duplicate`) instead
of a first write. Truncate `shipping.rate_policies` / `shipping.coverage_grants` to re-run cleanly.

## Admin authorization seeding (no bypass)

1. `GET /v1/admin/dev-context` — the Host's own **Development-only** seam that returns the sanctioned dev
   admin actor (created by `AdminDevActorBootstrap`, with a real `user#member@tenant:store-alpha` tuple).
2. `POST /v1/admin/access-control/bootstrap` — the product's **own** access-control bootstrap: it creates
   the system `platform-admin` role with the real permission catalog and assigns it to the actor, which
   writes the real `user#granted@permission:<id>` tuples through `IAuthorizationTupleWriter`.
3. `GET /v1/admin/access-control/me/capabilities` — proves the granted capabilities come from persisted
   grants (observed: 55 grants, including `fulfillment.view` and `fulfillment.manage`).

No authorization is disabled, no header is fabricated, no global allowlist or privilege fallback exists:
anonymous → `401 admin.actor.missing`, unknown actor → `403 admin.authorization.denied`.

## Scratch fixtures (scratch database only, no schema change)

* Shipping schema: the Development Host deliberately does **not** migrate the Shipping schema
  (`ShippingModuleCompositionGuardTests` asserts the module registers no `IModuleSchemaMigrator`).
  `seed-scratch-157.ps1` therefore publishes and runs the real product tool
  `Tooba.MigrationRunner apply --all-tenants` against the scratch target, which creates `shipping.*`
  (and `geography.*`).
* `scratch-geography-fixture-157.sql`: exactly one consistent destination chain
  (country → administrative area → city → service area) plus its names. The Shipping write seam validates
  its destination through the read-only Geography contract (`IGeographyLookup.GetByIdsAsync`), so an empty
  scratch geography cannot express a real scope (`shipping.destination_scope.invalid`). This is fixture
  data in a disposable database, not a schema or production change.
