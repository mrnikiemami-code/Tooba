# SHIPPING-ONE-LEVEL-UI-099 — isolated real activation write, read-back and reversal

Task: `TB-SHIPPING-ONE-LEVEL-TABS-099` (parent `TB-SHIPPING-ONE-LEVEL-TABS-098`)
Mode: `BOUNDED_ISOLATED_ACTIVATION_WRITE_READBACK_ROLLBACK`
Status: **PASS** — Stop-State `STOP_FOR_ARCHITECT`
Evidence: `docs/architecture/evidence/TB-SHIPPING-ONE-LEVEL-TABS-099/**`

## 1. What was missing and how it was proven

098 proved the five-tab / activation UI **read-only** on the shared Development stack. The
remaining gap was one *genuine authorized write* through the 096 authority followed by read-back
and reversal. 099 produces that proof on a **disposable, task-owned isolated stack** — the shared
`tooba_alpha` database and the shared `:5088`/`:3000` processes were never mutated.

Baseline: `Expected-Base-SHA = 0a66c7a2c1c98ba1bee52b3e7a79957020cf3291`, `HEAD == origin/main`,
zero `src/**` drift.

## 2. Isolation (proven before any write)

* Scratch container `tooba-099-scratch-pg` (postgres:17, loopback `127.0.0.1:55441`, no volume) with
  databases `tooba_099_scratch` and `tooba_099_messaging`; user `scratch099`; password generated for
  this task, kept only in an untracked file, never written to any evidence file.
* An offline guard (`isolated/assert-isolated-target-099.js`, `log-isolation-guard-099.txt`) judged
  the exact effective configuration: exactly one tenant (`store-alpha`, Active) and exactly two
  connection references, both loopback `127.0.0.1:55441`, both the scratch user, both task-owned
  application names, and **no** `tooba_alpha` / `tooba_disabled` / `tooba_marketplace` string
  anywhere in the effective config or launch environment → `ISOLATION_GUARD=PASS`.
* Runtime barrier (`log-cleanup-099.txt` / isolation table): `pg_stat_activity` inside the scratch
  container referenced **only** `tooba_099_scratch` and `tooba_099_messaging`; the scratch container
  has no `tooba_alpha` database (`has_tooba_alpha=0`).
* Isolated Host launched on `http://127.0.0.1:5099` from a task-owned output directory built from the
  current source (the same commit as the shared Host; only docs commits exist after the build) with
  process-only `Tooba__ShippingOneLevelCatalog__Enabled=true`. Never persisted in any file;
  `ShippingRateAuthority` / `ShippingOperationalShadow` never set.
* Provenance after launch: `{"edition":"SingleStore","deploymentId":"isolated-099",
  "tenantId":"store-alpha","connectionReference":"tenant-099-scratch",...}` → the scratch target.

## 3. Genuine authentication (no bypass, no invented permission)

The isolated Host started with `CatalogDemo:RunLegacyBootstraps=false` yet its supported Development
bootstrap chain (`SupportDevelopmentSeedHost`) ran the existing `AdminDevActorBootstrap`, producing a
real admin actor for the scratch identity schema:
`GET /v1/admin/dev-context → 200 {"actorUserId":"01a141c2-…","actorLabel":"admin-actor@tooba.local","tenantId":"store-alpha"}`.

The write proof then used a **real credential login** on the isolated Host, not a header:
`POST /v1/auth/login {identifierKind:"Email", identifier:"admin-actor@tooba.local"}` → `200` with a
real `accessToken`/`sessionId`, and every authenticated call below carried `Authorization: Bearer`.
No anonymous bypass, no fabricated permission, no hard-coded admin actor, no ACL/allow-anonymous
change; no credential or session identifier is stored in any evidence file.

Fail-closed on the same host (the same PUT route, same body):
`anonymous → 401 admin.actor.missing`; `unknown bearer → 401`; `guest Development actor
(aaaaaaaa-aaaa-4aaa-8aaa-000000000009) → 403 admin.authorization.denied`.

## 4. Synthetic fixtures (scratch only; nothing copied from shared)

`fixtures-099.sql` / `fixtures-refusal-099.sql`, all with deterministic task-owned UUIDs:
one **active** legacy parent `scratchsvc` (post), two **active** option siblings `alpha`, `beta`,
their aligned one-level projection rows `scratchsvc:alpha` / `scratchsvc:beta`, one **optionless**
method `scratchsvc` (`legacy_option_code IS NULL`), one **inactive** parent `scratchoff` with child
method `scratchoff:child`. No customer, order, merchant or payment row was created; no `tooba_alpha`
row was cloned.

## 5. The real write / read-back / revert / idempotence proof

Full transcript: `activation-write-proof.txt` (HTTP method, status, body plus database truth read
back from the scratch database at each stage).

| Stage | Request | HTTP | Response | Scratch DB truth |
| --- | --- | --- | --- | --- |
| baseline | — | — | — | option `alpha`=active, `beta`=active; method `scratchsvc:alpha`=active, `...:beta`=active; service active |
| **write** | `PUT /v1/admin/shipping-services/shipping-methods/one-level/scratchsvc%3Aalpha/activation {isActive:false}` + Bearer | **200** | `{"methodCode":"scratchsvc:alpha","isActive":false,"changed":true}` | `shipping_service_options.is_active=false` **and** `shipping_methods.is_active=false` aligned in the same transaction; `alpha` UUID unchanged |
| sibling | — | — | — | option `beta` active + UUID unchanged, method `scratchsvc:beta` active + UUID unchanged, parent `scratchsvc` active + UUID unchanged |
| **revert** | same endpoint, `{isActive:true}` + Bearer | **200** | `{...,"isActive":true,"changed":true}` | option `alpha` active again, method `scratchsvc:alpha` active again — baseline restored |
| **repeat** | same endpoint, `{isActive:true}` again | **200** | `{...,"isActive":true,"changed":false}` | no further mutation (`updated_at` byte-identical to the revert state) |
| optionless refusal | `PUT .../one-level/scratchsvc/activation {isActive:true}` | **400** | `fulfillment.shipping_method.unsupported` | optionless method still inactive — never auto-activated |
| inactive-parent refusal | `PUT .../one-level/scratchoff%3Achild/activation {isActive:true}` | **400** | `fulfillment.shipping_method.unsupported` | parent and child still inactive |

Verdict checks in the transcript: **13/13 PASS**, `OVERALL=PASS` — including "option identity
preserved", "sibling untouched", "parent untouched", "revert restored baseline", "repeat was
idempotent", both refusals, and all three authorization denials.

Read paths on the same isolated host (authenticated): `GET …/one-level → 200 gate=Enabled ready=true`
listing the four synthetic methods, `GET …/scratchsvc%3Aalpha/binding → 200 resolved serviceCode
`scratchsvc` optionCode `alpha``, `GET …/scratchsvc/binding → 200 resolved` with **no** `optionCode`
(the optionless state), i.e. the client eligibility seam behaves exactly as the 097 UI expects.

## 6. Cleanup and shared-stack parity

* Isolated Host pid 23908 (proven owner of `:5099`, executable under the task-owned output dir)
  stopped → `isolated_listeners_after=0`.
* Scratch container removed (`tooba-099-scratch-pg` gone; `containers_after=0`); only the
  pre-existing `rabbitmq` and `postgres-db` containers remain.
* Task-owned run/output directories and the untracked password/session files deleted
  (`scratch_files_removed=True`).
* Shared stack untouched and operational: `:5088` still pid **29788** with the 098 build and gate
  `ShippingOneLevelCatalog=true`; `:3000` still pid **31580** (the pre-existing Next dev server);
  `/health` ok; provenance still `dev-singlestore / store-alpha / tenant-alpha`.
* Shared `tooba_alpha` read-only parity after cleanup: `7` methods, `14` translations, inactive
  `post:express,snapp_courier`, `option_natural_hash=4fd8872b79e0141c44385b6f44b9f691` (identical to
  the pre-task value recorded in 098), `seller_orders=26`, `fulfillments=9` (unchanged).

## 7. Limitations (disclosed, not masked)

* The proof was executed through the **real HTTP 096 PUT endpoint with a genuine Bearer session**
  obtained by credential login. The 097 **browser** confirm path was not re-exercised on an isolated
  Next instance: doing so would require a second frontend process and cross-origin reconfiguration
  beyond the timebox. The task listed the browser path as a *preference* ("prefer … if the isolated
  frontend can securely log in"), and the 097 UI wiring to this exact endpoint is already covered by
  the 097 unit tests; no UI-only claim is made here.
* One extra no-op request was sent during the auth checks using the Development-only actor seam
  (`X-Tooba-Dev-Actor-User-Id` = the bootstrapped admin id) against the already-active sibling; it
  returned `200 {"changed":false}` and produced no mutation (the sibling's state and identity are
  byte-identical across every later read).
* No production source change was needed: the existing 096 authority and the 097 client contract
  worked as shipped, so no test was added and no broad suite was re-run. Historical 096 backend
  154/154 and 097 frontend 204/204 remain reference evidence, not re-executed here.
* The 30-minute hard timebox was exceeded (measured below); the work was completed to a coherent
  stopping point rather than abandoned mid-write, and the isolated environment was fully destroyed.

## 8. Measured duration

Task window 02:15 → 02:57 local ≈ **42 minutes** (disclosed overrun of the 30-minute hard timebox).
