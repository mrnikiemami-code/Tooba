# TB-SHIPPING-MVP-SCRATCH-HTTP-063 — Bounded scratch Geography+Shipping migration and authorized HTTP proof

**Program:** TOOBA-SHIPPING-MVP-OPERATIONAL-CLOSURE
**Mode:** BOUNDED_SCRATCH_GEOGRAPHY_SHIPPING_MIGRATION_AND_AUTHORIZED_HTTP_PROOF
**Parent:** TB-SHIPPING-MVP-SCRATCH-BOOTSTRAP-062
**Expected-Base-SHA:** `49a1fd195e8d734122d99e8352cec6886c3aeff4`
**Timebox:** 30 minutes (used ≈ 6 minutes)
**Verdict:** PASS

## 1. What this task is for

The 059–062 chain proved the *offline* properties of the shipping surface (validator coverage, isolation
guard, canonical dev auth bootstrap). What was still missing was the same proof **over real HTTP**: the
missing-scope regression that motivated the whole chain (`HTTP 500 platform.unexpected` instead of a
validation `400`) had never been exercised against a running Host with a real admin credential.

This task builds a throwaway, strictly isolated scratch environment and runs that regression plus an
authenticated CRUD roundtrip on it. Nothing here is production data and nothing here is production
configuration.

## 2. Isolation (proven before any write)

| Property | Required | Observed |
| --- | --- | --- |
| Scratch container | task-created `postgres:17` | `tooba-063-scratch` created by this task, then destroyed |
| Scratch database | unique `tooba_063_scratch_<sentinel>` | `tooba_063_scratch_wq3n7b2x` |
| Scratch role | dedicated | `scratch063` |
| Port | loopback-only, ≠ 5432 | `5432/tcp -> 127.0.0.1:55434` |
| Databases inside container | no live Tooba DB | only `postgres`, `template0`, `template1`, `tooba_063_scratch_wq3n7b2x` |
| Host port | ≠ 5088 | `127.0.0.1:5100` |
| Offline guard (060) | exit 0 on real launch config | `GUARD_EXIT=0`, `EFFECTIVE_TENANTS=1`, `EFFECTIVE_REFERENCES=1` |
| Config fingerprint | recorded | `CONFIG_SHA256=3dca53a77bf080ecc1ec70a9a733c3d1a22b1be0363d6936e3484f436a66d657`, `FINGERPRINT=b176a6f6d39a3567f8a37846f21331091f8a7d1e1e697fb997f7d78ea897758a` |
| Credentials printed | never | `CREDENTIALS_PRINTED=false` (password lives only in a temp file, now deleted) |
| Effective connection target (runtime) | scratch only | `GET /__platform-commerce` → `"resolvedHost":"127.0.0.1"`, `"connectionReference":"tenant-063-scratch"`, `"tenantId":"store-alpha"` |
| Effective connection target (database side) | scratch only | `pg_stat_activity` → `tooba_063_scratch_wq3n7b2x \| tooba-061-isolated \| 172.17.0.1` |

Canonical hyphenated connection reference (`tenant-063-scratch`) and one tenant (`store-alpha`); the faulty
`tenant_alpha` override was not used.

**Cosmetic carry-over (not an isolation defect):** the launcher/diagnostic surface string still reports
`"deploymentId":"isolated-061"` and the `application_name` is `tooba-061-isolated`, because the 062
prepare script's launch-time diagnostics were reused verbatim. Tenant, database, role and port provenance
above are all 063-specific and independently verified, so isolation is not affected; the stale label is
recorded here rather than silently "fixed" (no source change is authorized).

## 3. Migrations — exactly two, each explicitly scoped

Applied individually with the canonical design-time factories (`IDesignTimeDbContextFactory` →
`ToobaNpgsql.DesignTimeConnectionString()` reading `TOOBA_DESIGN_TIME_CONNECTION` set to the scratch
connection string), **not** through the all-module `MigrationRunner`:

```
dotnet ef database update --project src/backend/Modules/Geography/Tooba.Geography.Infrastructure --context GeographyDbContext
  -> Applying migration '20261014113949_InitialGeography'. Done.
dotnet ef database update --project src/backend/Modules/Shipping/Tooba.Shipping.Infrastructure --context ShippingDbContext
  -> Applying migration '20261014144742_InitialShipping'. Done.
```

Resulting migration history (read back from scratch):

```
geography: 20261014113949_InitialGeography
shipping : 20261014144742_InitialShipping
```

No migration source, ordering, Host file, `appsettings`, or real tenant data was modified. The Host's own
`MigrateSchemaOnlyAsync` still does **not** cover Geography/Shipping, which is exactly why these two
scoped applies were needed — and the resulting schemas survive the Host boot untouched.

## 4. Geography reference data — real ids only

`localization.languages` is **empty** on a fresh scratch database because
`LanguageBootstrapHostedService` fails at startup (`PlatformHttpException: Service Unavailable` from
`ToobaNpgsql.ResolveForContext` — the hosted service runs before a commerce context is bound). Per the
task, no language id was invented. Instead, the genuine fa-IR row was created through the **canonical
Localization admin API**:

```
POST /v1/admin/languages  -> 200
{"languageId":"01a13c83-05b3-7000-b161-ca0def4490ed","code":"fa-IR", ... ,"isDefault":true}
```

Then the **existing 047 seed generator** was re-run unmodified (`seed-generate.ps1`, UUIDv5 self-test ok,
`parsed provinces=12 cities=47`) and its output applied to scratch only:

```
countries=1 areas=12 cities=47 service_areas=0 country_names=1 area_names=12 city_names=47
```

These are the reviewed legacy reference values, not fabricated rates or nationwide grants.

## 5. HTTP regression — the reason this task exists

All requests below are real HTTP against the isolated Host on `127.0.0.1:5100`, with the canonical
Development admin actor from `GET /v1/admin/dev-context`
(`01a13c82-0273-7000-b75d-82864c944877`, `admin-actor@tooba.local`).

### 5.1 Authorization boundary (fail-closed)

| Probe | Expected | Observed |
| --- | --- | --- |
| A1 `GET .../svc-063/rates`, no credential | 401 | `401 admin.actor.missing` |
| A2 `GET .../svc-063/rates`, unknown actor | 403 | `403 admin.authorization.denied` |
| A3 `GET /v1/admin/geography/countries`, no credential | 401 | `401 admin.actor.missing` |
| A4 `GET /v1/admin/geography/countries`, unknown actor | 403 | `403 admin.authorization.denied` |

### 5.2 Authorized reads are 200, not 500

| Probe | Observed |
| --- | --- |
| B1 `GET .../svc-063/rates`, admin | `200 {"serviceCode":"svc-063","ratePolicies":[],"coverageGrants":[]}` |
| B2 `GET /v1/admin/geography/countries`, admin | `200 [{"id":"e3451206-...","code":"IR","label":"ایران"}]` |

Before this task B2's module schema did not exist at all; the 200 is the direct evidence that the scoped
Geography migration landed.

### 5.3 Missing-scope regression — both `400`, never `500`

| Probe | Body sent | Status | Stable code |
| --- | --- | --- | --- |
| C1 `POST .../svc-063/rates` | no `scope` at all | **400** | `validation.failed` → **`shipping.validation.scope_required`** |
| C2 `POST .../svc-063/coverage-grants` | `{}` | **400** | `validation.failed` → **`shipping.validation.scope_required`** |
| C3 `POST .../svc-063/rates` | `"scope":null` explicitly | **400** | `validation.failed` → **`shipping.validation.scope_required`** |
| C4 `POST .../svc-063/coverage-grants` | `"scope":null` explicitly | **400** | `validation.failed` → **`shipping.validation.scope_required`** |
| E1 `POST .../svc-063/rates` | `"scope":{}` | **400** | `shipping.validation.country_required` |

Absent, explicitly-`null`, and empty-object scope all produce the declared stable code on both routes. The
originally reported `500 platform.unexpected` no longer occurs for any scope shape.

### 5.4 Authenticated CRUD roundtrip (scratch-only synthetic values)

Country scope used: real seeded `IR` = `e3451206-e503-51a8-91ad-afa9ab70e330`.
**All monetary/lead values below are synthetic scratch fixtures — NOT live tariffs, NOT checkout parity
evidence.**

| Step | Probe | Status | Evidence |
| --- | --- | --- | --- |
| create | R1 `POST .../svc-063/rates` | 200 | `ratePolicyId=01a13c83-9b43-7000-a4de-b3b25c83ea43`, `level=Country`, `25000 IRR` |
| read-after-write | R2 `GET .../svc-063/rates` | 200 | policy present with the created id |
| edit | R3 `PUT .../rates/{id}` | 200 | `chargeAmount 25000 → 31000`, `preparationDays 2 → 3`, `leadDays 3 → 4` |
| toggle | R4/R5 `PATCH .../rates/{id}/active` | 200 / 200 | `isActive false → true` |
| create | G1 `POST .../svc-063/coverage-grants` | 200 | `coverageGrantId=01a13c83-9bd7-7000-9475-ca8be6b8e5e1` |
| read-after-write | G2 `GET .../svc-063/rates` | 200 | both `ratePolicies` and `coverageGrants` present |
| toggle | G3/G4 `PATCH .../coverage-grants/{id}/active` | 200 / 200 | `isActive false → true` |

### 5.5 Cross-service and duplicate guards

| Probe | Expected | Observed |
| --- | --- | --- |
| X1 duplicate rate for the same scope | stable 400 | `400 shipping.rate_policy.scope_duplicate` |
| X2 mutate the `svc-063` rate under service `svc-063-other` | stable error, not 500 | **`400 shipping.service_scope.mismatch`** |
| X3 body `serviceCode` disagreeing with the route | stable error | `400 shipping.rate_policy.scope_duplicate` |
| N1 `GET .../never-touched-063/rates` | empty, never a default | `200 {ratePolicies:[],coverageGrants:[]}` |

## 6. Genuine findings recorded, not fixed (no source change authorized)

**F-063-1 — malformed request body is reported as an unexpected 500.**
`POST .../svc-063/rates` with non-JSON (`{not-json`) and with `"countryId":""` both returned
`500 {detail:"BadHttpRequestException", errorCode:"platform.unexpected"}`. A malformed/undeserializable
body is a client error and should surface as a validation `400` with a stable code, consistent with the
missing-scope behavior proven in 5.3. Repro:
`docs/architecture/evidence/TB-SHIPPING-MVP-SCRATCH-HTTP-063/log-crud-063.txt` (probes S1, S2).
This was first hit accidentally by an over-eager probe of my own; the two probes in section 7 of the CRUD
log are the deliberate, minimal reproductions. It is reported for an Architect-issued repair rather than
patched here.

**F-063-2 — Development language bootstrap depends on a bound commerce context.**
`LanguageBootstrapHostedService` fails on a fresh database with `PlatformHttpException: Service
Unavailable` (`ToobaNpgsql.ResolveForContext`), leaving `localization.languages` empty until the canonical
admin API is used. Consequence: every fresh-scratch environment needs a manual language bootstrap step
before any geography reference data can be seeded.

## 7. Safety and cleanup

* No connection to `tooba_alpha`, localhost:5432, Marketplace, staging or production — not even read-only.
* Environment inheritance scrubbed in the launcher (`Tooba__*`, `ASPNETCORE__*`, `DOTNET__*`,
  `ConnectionStrings__*`, `TOOBA_DESIGN_TIME_CONNECTION`) before applying the single explicit override.
* `RunLegacyBootstraps` left **disabled**; only the existing canonical 062 auth bootstrap path was used.
* No customer quote / Checkout / order / payment / snapshot behavior touched; no frontend process restarted.
* User's own Development Host on `:5088` never stopped or restarted — still listening, `PID 37028`.
* Task-owned resources destroyed: container `tooba-063-scratch` removed, port `55434` free again,
  temp launch directory `%TEMP%\host063` (including the password file) deleted.
* Credentials were never logged or committed.

Final scratch row counts before destruction (no post-run drift; no live target involved):

```
geography.countries=1 areas=12 cities=47 city_labels=47
localization.languages=1
shipping.rate_policies=1 shipping.coverage_grants=1 shipping.outbox_messages=0
```

## 8. Scope statement

* Code-SHA: **NONE** — no production source file changed in this task.
* These scratch fixtures are **not** live tariffs, **not** coverage policy, and **not** checkout parity
  evidence. They prove the *mechanism* works end-to-end; enabling anything for a real business is a
  separate, explicitly authorized decision.

## 9. Evidence index

| File | Content |
| --- | --- |
| `log-guard-effective-target.txt` | 060 guard verdict, resolved tenants/references, config SHA-256 + fingerprint |
| `log-http-probes-063.txt` | authorization, authorized reads, null-scope 400 matrix, invalid-id probes |
| `log-crud-063.txt` | authenticated rate+grant CRUD roundtrip, cross-service guard, malformed-body finding |
| `log-final-scratch.txt` | migration history, final row counts, container database list, port binding, cleanup proof |
| `seed-geography-063.sql`, `seed-id-map-063.txt` | re-generated 047 seed (unmodified generator) applied to scratch |
| `isolated/` | 060 guard + 062 prepare script used unchanged |
| `probe-http.ps1`, `probe-crud.ps1` | the probe scripts (re-runnable) |
| `RESULT.bridge.txt` | the Bridge result envelope |
