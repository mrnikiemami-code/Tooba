# TB-SHIPPING-MVP-ISOLATED-HTTP-061 — isolation proof, real-HTTP probes, honest verdict

- Program: TOOBA-SHIPPING-MVP-OPERATIONAL-CLOSURE
- Parent: TB-SHIPPING-MVP-SAFETY-REPAIR-060
- Mode: STRICTLY_ISOLATED_AUTHENTICATED_HTTP_AND_NULL_SCOPE_REGRESSION
- Expected base: `60ce0b14d53be693d6d9d53d4119adfa16ec5d37`
- Observed base: `4d50587cc3c001f7146b6c3ccb54fba671ebcf96`
- Stop state: STOP_FOR_ARCHITECT
- Automatic next implementation task: NONE

## Verdict

**PARTIAL.** The mandatory isolation gate was satisfied completely and independently: the launched process was
proven — offline, at runtime, and from the database side — to be addressing only a task-created disposable scratch
database, with no reachable path to `tooba_alpha` or any other real Tooba database. The fail-closed authorization
boundary was also proven over real HTTP (401 / 403). The central regression this task exists to measure — the two
missing-scope POSTs returning a canonical `400 validation.failed` with `shipping.validation.scope_required` over
actual HTTP — could **not** be measured, because in a genuinely fresh isolated database the supported Development
admin bootstrap does not provision any capability, so every authorized admin surface answers `403` before the
validation pipeline is reached. No production source was changed; no real database was contacted.

## 1. Preflight

`git fetch origin`; HEAD == origin/main == `4d50587cc`. Expected-Base-SHA `60ce0b14d` is an ancestor and
`base..HEAD` is exactly one commit whose only deltas are
`docs/architecture/evidence/TB-SHIPPING-MVP-SAFETY-REPAIR-060/RESULT.bridge.txt` and `post-result.js` — a
path-by-path evidence-only bookkeeping commit with zero source overlap. No RECOVERY_CONFLICT. No reset, stash,
rebase or force-push; the unrelated dirty/untracked paths present in the workspace were never staged.

The received task artifact was preserved verbatim as an unstaged receipt at
`docs/ai/tasks/TB-SHIPPING-MVP-ISOLATED-HTTP-061.task.md`.

## 2. Why the isolation design is different from 059

059 failed because a mis-spelled override key (`tenant_alpha` instead of the hyphenated `tenant-alpha`) was
silently ignored and the process fell through to the real Development connection string. This run removes the
possibility structurally rather than by careful naming:

| Layer | Measure |
| --- | --- |
| Container barrier | a NEW task-exclusive `postgres:17` container (`tooba-061-scratch`), own data volume, published only as `127.0.0.1:55432`. `tooba_alpha` does not exist inside it, so it is unreachable from this process by construction. |
| Configuration barrier | the launch directory is task-owned and its `appsettings.Development.json` was replaced with a task-owned overlay declaring exactly one Active tenant and exactly one reference. The real Development configuration file never participates. |
| Base validation | the prepare step refuses to continue unless the base `appsettings.json` declares 0 tenants and 0 connection references, and asserts the merged effective configuration still has exactly 1 tenant / 1 reference. |
| Naming | canonical hyphenated key `tenant-061-scratch` in both the tenant definition and the reference map, plus a hyphenated env override, so the 059 spelling failure is impossible. |
| Preflight | the copied 060 guard runs over the exact (effective config, override file) pair, with an expected sentinel and a fingerprint, before anything starts. |
| Runtime provenance | `GET /__platform-commerce` reports the reference the process actually resolved. |
| Database-side barrier | the scratch container's `pg_stat_activity` shows the Host's backends identified by their unique `Application Name`, connected only to the scratch database. |

Environment hygiene: the launcher clears all inherited `Tooba__*`, `ASPNETCORE_*`, `DOTNET_*` and
`ConnectionStrings__*` before setting only the isolated values, and no user-secrets id exists in the Host project,
so no ambient configuration could re-introduce a real connection.

## 3. What was proven

1. Offline preflight over the effective configuration: **PASS** (`EXIT_CODE=0`), fingerprint and its exact config
   SHA-256 re-verified by the caller, `user=tests only`, `CREDENTIALS_PRINTED=false`, `DATABASE_CONTACTED=false`.
2. The copied guard's own synthetic fixture suite still passes 12/12, so the copy is not a weakened guard.
3. Runtime provenance: `/__platform-commerce` → `connectionReference=tenant-061-scratch`, `tenantId=store-alpha`,
   `deploymentId=isolated-061`.
4. Database-side barrier: the Host's sessions appear only in `tooba_061_scratch_k7q2m9zd`.
5. Fail-closed auth: no credential → `401 admin.actor.missing`; provisioned actor without capabilities →
   `403 admin.authorization.denied` on every admin route tried.
6. Cleanup: task-owned Host stopped, `:5098` free, scratch container deleted with no volumes left, temp launch
   directory and credentials removed. The user's `:5088` Host has the same PID (`37028`) before and after and still
   answers `/health` with `{"status":"ok"}`. No real database was queried, edited or cleaned.
7. Nothing commercial was written anywhere: every write attempt was denied, so no rate, grant, nationwide default
   or invented coverage exists.

## 4. What could not be proven, and why

The mandatory isolation gate passed, so the Host was legitimately started and probes were legitimately sent. Two
conditions then blocked the HTTP regression:

- **Capability provisioning is absent in a fresh isolated database.** After two clean starts,
  `access_control.roles = 0`, `role_permissions = 0`, `user_role_assignments = 0`. The Development actor bootstrap
  did run — `GET /v1/admin/dev-context` returns the actor — but the access-control prelude never established the
  `platform-admin` role or its permission tuples. The supported HTTP bootstrap route cannot be used to break the
  deadlock because it itself requires the missing capability (`403`). Consequently every authorized admin surface
  answers `403` **before** the request reaches the MediatR validation pipeline, so the repaired null-scope path
  cannot be observed over HTTP here. This is a limitation of the environment, not a contradiction of the 060 fix.
- **The alternative supported Development seeding switch is broken.** Setting
  `Tooba:CatalogDemo:RunLegacyBootstraps=true` aborts the Host during startup with
  `System.InvalidOperationException: "محصول برای انتشار آماده نیست."` raised from the Catalog legacy seed.

Both are directly reproduced, pre-existing behaviours. The task explicitly says not to widen scope for a
newly discovered blocker without Architect direction, so neither was repaired and the SHA of the production source
is unchanged.

A third, related environment fact is recorded for the Architect: `DevelopmentSchemaMigrator.MigrateSchemaOnlyAsync`
created 29 schemas but not `geography`, `shipping`, `returns` or `settlement`, and the Outbox dispatcher repeatedly
logged missing `returns.outbox_messages` / `settlement.outbox_messages`. A fresh isolated database therefore cannot
exercise the Shipping/Geography admin surface even with an authorized actor.

## 5. Acceptance mapping

| Acceptance condition | Result |
| --- | --- |
| isolated effective target independently proven before requests | **PASS** (offline guard + runtime provenance + database-side barrier) |
| BOTH real missing-scope POSTs return 400 with the stable validation code | **NOT MEASURED** — requests are denied with 403 before validation |
| authenticated rate/grant CRUD roundtrips entirely within scratch | **NOT MEASURED** — capabilities absent in a fresh isolated DB |
| unauthorized access denied | **PASS** (401 / 403, fail-closed) |
| cleanup proven, no existing Host or database touched | **PASS** |
| PASS only if all of the above hold | therefore **PARTIAL**, never claimed as PASS |

The 060 offline evidence (focused green unit tests proving the declared `shipping.validation.scope_required`
failure and its translation to the canonical `validation.failed` 400 envelope) remains the strongest available
evidence for the null-scope repair, but it is explicitly **in-memory** evidence and is not re-labelled as live.

## 6. Changed paths

Only the task's allowed paths:
`docs/planning/shipping/SHIPPING-MVP-ISOLATED-HTTP-061.md`,
`docs/architecture/evidence/TB-SHIPPING-MVP-ISOLATED-HTTP-061/**`, and the unstaged receipt
`docs/ai/tasks/TB-SHIPPING-MVP-ISOLATED-HTTP-061.task.md`. `Code-SHA = NONE` — no production source was modified.
