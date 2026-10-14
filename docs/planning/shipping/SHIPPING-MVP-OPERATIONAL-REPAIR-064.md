# TB-SHIPPING-MVP-OPERATIONAL-REPAIR-064 — malformed-body mapping and fresh-DB language bootstrap

**Program:** TOOBA-SHIPPING-MVP-OPERATIONAL-CLOSURE
**Mode:** BOUNDED_SHIPPING_MALFORMED_BODY_AND_FRESH_LANGUAGE_BOOTSTRAP_REPAIR
**Parent:** TB-SHIPPING-MVP-SCRATCH-HTTP-063
**Expected-Base-SHA:** `81dea23234841d6dade3eca6dd0b808080a0933b`
**Timebox:** 30 minutes
**Verdict:** PASS

This task closes the two defects that 063 reported but did not repair, without changing shipping
semantics, Checkout, commercial policy, or Host composition.

---

## Mission A — F-063-1: malformed body surfaced as `500 platform.unexpected`

### Root cause (traced before any edit)

The five Shipping admin write routes bound their request body as a **minimal-API parameter**
(`ShippingRatePolicyWriteRequest body`, `ShippingCoverageGrantWriteRequest body`, …). ASP.NET Core
resolves that parameter *before the handler runs*. When the payload could not be deserialized
(syntax-invalid JSON, a non-`Guid`/empty GUID in `scope`, a non-JSON content type) the framework threw
`BadHttpRequestException` **outside** the endpoint.

`SafeErrorMapper.Map` has no branch for that type — it classifies only `SemanticException`,
`ValidationException` and `PlatformHttpException`, and everything else falls through to
`MapUnexpected()`:

```text
SafeErrorMapper.Map(exception) =>
    SemanticException   -> Map(semantic)
    ValidationException -> MapValidation(validation)
    PlatformHttpException -> MapPlatform(platform)
    _                   -> MapUnexpected()  // -> 500 platform.unexpected
```

So a pure client-input failure became an unexpected server error. The 063 evidence
(`log-crud-063.txt`, probes S1/S2) is the reproduction: `500 {"detail":"BadHttpRequestException",
"errorCode":"platform.unexpected"}`.

### Why not the global exception handler

`SafeErrorMapper`/`ExceptionPresentationService`/`ToobaExceptionHandler` are Host-owned global
mapping. Classifying `BadHttpRequestException` there would change every module's error surface at
once — far beyond this task's ownership, and explicitly forbidden without separate authorization. The
defect is owned by the Shipping admin boundary, so the repair lives there.

### Why not an endpoint filter

An `IEndpointFilter` looks like the natural place to catch this. It is not: minimal-API **argument
binding runs before the filter pipeline**, so a filter never sees the binding failure. Verified
empirically with a throwaway minimal app before choosing the design (binding failure returned the
framework's own `400`, never reaching a filter that would have answered `418`).

### Repair

`ShippingRateCoverageEndpoints.cs` now reads the body itself, after authorization:

- `TryReadBodyAsync<T>` uses `HttpRequest.ReadFromJsonAsync<T>` — the same in-repo precedent as
  `StorefrontOrderEndpoints` — and returns `null` for a non-JSON content type, `JsonException` and
  `BadHttpRequestException`.
- `ReadBodyAsync<T>` turns a `null` result into the **declared validation envelope** through the
  existing `ApiResponseFactory.FromException`, using a `ValidationException` carrying
  `ValidationFailure(propertyName: "request", errorCode: shipping.validation.body_invalid)`.
- Authorization still runs **first**, so an unauthenticated or unauthorized request stays `401`/`403`
  and a malformed body can no longer pre-empt the access decision.

New declared code (`ShippingValidationCodes.BodyInvalid`):
`shipping.validation.body_invalid`. It travels inside the pre-existing foundation
`validation.failed` 400 descriptor, exactly like every other `shipping.validation.*` transport code —
those are intentionally **not** registered as error-catalog descriptors in this repository
(the documented convention), and Shipping owns no `.resx` pair, so no localization surface was
invented for one code.

No global handler, Host file, migration, or unrelated module was touched. `Results.*` is still unused;
the canonical `ApiResponseFactory` produces the envelope.

### Proof (isolated scratch Host, real HTTP)

See `log-http-064.txt`. Every write route, every unreadable-body shape:

| Probe | Request | Status | Envelope |
| --- | --- | --- | --- |
| S1 | `POST .../rates` `{not-json` | **400** | `validation.failed`, `errors.request=[shipping.validation.body_invalid]` |
| S2 | `POST .../rates` `scope.countryId=""` | **400** | same |
| S3 | `POST .../rates` `scope.countryId="not-a-guid"` | **400** | same |
| S4 | `POST .../rates` literal `null` | **400** | same |
| S5 | `POST .../rates` `text/plain` body | **400** | same |
| S6 | `POST .../coverage-grants` `{oops` | **400** | same |
| S7 | `PUT .../rates/{id}` `{bad` | **400** | same |
| S8 | `PATCH .../rates/{id}/active` `{bad` | **400** | same |
| S9 | `PATCH .../coverage-grants/{id}/active` `{bad` | **400** | same |

Unchanged behavior, re-proven on the same run:

| Probe | Request | Status |
| --- | --- | --- |
| A1/A2 | `GET .../rates` without credential / unknown actor | **401** `admin.actor.missing` / **403** `admin.authorization.denied` |
| A3/A4 | `POST .../rates` malformed body without credential / unknown actor | **401** / **403** |
| M1 | `POST .../rates` no `scope` | **400** `Request.Scope=[shipping.validation.scope_required]` |
| M2 | `POST .../coverage-grants` `{}` | **400** `shipping.validation.scope_required` |
| M3 | `POST .../rates` `scope:{}` | **400** `shipping.validation.country_required` |
| B1/B2 | authorized `GET` rates / geography countries | **200** |
| V1–V4 | valid rate + grant create and read-after-write (synthetic scratch values) | **200** |

**Intentional documented delta:** a non-JSON/absent content type now yields `400
shipping.validation.body_invalid` instead of the framework's `415`. Both are client-error class, and
authoring a raw `415` would require a non-canonical `Results.*` in the endpoint (the repo's
API-result convention forbids it). The 063-mandated cases are unaffected.

---

## Mission B — F-063-2: fresh Development database had zero languages

### Root cause

`LanguageBootstrapHostedService` is an `IHostedService`: it runs during host start, **before** any
commerce context is bound. It resolved `LocalizationDbContext`, whose factory calls
`ToobaNpgsql.ResolveForContext`, which fail-closes with `503 platform.edition.unconfigured` when
`ICurrentCommerceContext.Current` is null:

```text
ToobaNpgsql.ResolveForContext: commerce.Current ?? throw new PlatformHttpException(503, ...)
```

The assignment performed by `DevelopmentSchemaMigrator.ApplyCoreAsync`
(`ICommerceContextAssigner.Assign(...)`) lives in that method's own scope; the hosted service gets a
**different** scope, so its accessor is fresh and empty. The exception was swallowed by the service's
broad `catch`, logged at `Error` level, and the database stayed at zero languages. This is why 063 had
to create `fa-IR` by hand over the admin API.

(Production is not better off: the same hosted service also cannot resolve a connection there, because
no request context exists at startup either.)

### Repair — use the module-owned Development seam that already exists

`ModuleSchemaMigrator` (owned by `Tooba.Persistence`, a shared building block) already defines the
supported initialization order: `IModuleSchemaMigrationStep` runs *inside the scope where the Host has
assigned the commerce context*, immediately after its module's schema migration. Catalog and Promotion
already seed their Development prerequisites this way.

- New `Bootstrap/LanguageBootstrapDevelopmentSeed.cs` (Localization-owned):
  - `LanguageBootstrapRunner.EnsureSeededAsync` / `MigrateAndSeedAsync` — the idempotent
    migrate-then-seed body that previously lived inline in the hosted service.
  - `LanguageBootstrapDevelopmentSeed.PostMigration.EnsureAsync` — gates on
    `IHostEnvironment.IsDevelopment()` (same no-op-outside-Development shape as
    `CatalogDevelopmentSeed.PostMigration`) and then seeds through the **passed-in** provider, so the
    Host's assigned context is the one used. It does not migrate, because the Localization
    `IModuleSchemaMigrator` at order 22 already ran.
- `LocalizationModule.AddServices` registers it with
  `AddModuleSchemaMigrationStep("Localization", ModuleSchemaMigrationOrder.Localization, …)`.
- `LanguageBootstrapHostedService` now delegates to the runner and distinguishes "no commerce context
  at startup" (an expected, by-design condition → `LogInformation`) from a genuine failure
  (`LogError`). No masking: only the `PlatformHttpException` with status `503` is downgraded; every
  other exception still logs as an error. The class and its file stay in place — the AMC W2 structure
  guard asserts its path.

No language GUID is hardcoded, no DbContext/tenant resolution is bypassed, tenant isolation is
untouched, and legacy Catalog bootstraps remain disabled.

### Proof (isolated fresh scratch, `log-guard-effective-target.txt`, `log-restart-idempotence.txt`)

| Check | Observed |
| --- | --- |
| First start on an empty database, **no manual POST** | `localization.languages` = `fa-IR (default)`, `en-US` |
| `GET /v1/admin/languages` with the canonical admin actor | **200**, both rows, fa-IR `isDefault:true` |
| Second start (stop + start, same database) | still exactly 2 rows; no duplicate seeding |
| Startup log, first and second start | `Information` "Language bootstrap skipped: no commerce context is bound at host startup…" (no error) |
| Geography reference seed, which requires a real fa-IR language id | succeeded: `countries=1 areas=12 cities=47` with no invented id |

---

## Isolation and safety

| Property | Required | Observed |
| --- | --- | --- |
| Scratch container | task-created `postgres:17`, destroyed after | `tooba-064-scratch` |
| Scratch database | unique, non-live, only Tooba-named DB in container | `tooba_064_scratch_r7q4f9kd` (list: `postgres`, `template0`, `template1`, scratch) |
| Dedicated role / loopback port | ≠ 5432 | `scratch064`, `5432/tcp -> 127.0.0.1:55435` |
| Host port | ≠ 5088 | `127.0.0.1:5101` |
| 060 offline guard on the real launch config | exit 0 | `GUARD_EXIT=0`, `EFFECTIVE_TENANTS=1`, `EFFECTIVE_REFERENCES=1`, `CREDENTIALS_PRINTED=false` |
| Config fingerprint | recorded | `CONFIG_SHA256=c733af540f2263d4b95468372d05babb86ee05651d6a8b090328ecda25d71958`, `FINGERPRINT=9d197eec2160e4b0f9920b0f2168a1c1add4d412ee6d20eb161c892cb70f1ab4` |
| Effective target (runtime) | scratch only | `/__platform-commerce` → `"connectionReference":"tenant-064-scratch"`, `"resolvedHost":"127.0.0.1"` |
| Effective target (database side) | scratch only | `pg_stat_activity` → `tooba_064_scratch_r7q4f9kd \| tooba-061-isolated \| 172.17.0.1` |
| User's Development Host | never stopped/restarted | `:5088` still listening, PID 37028 |
| Cleanup | task-owned only | container removed, `55435` and `5101` free, temp launch dir (with password file) deleted |

Cosmetic carry-over, unchanged from 063: the launcher diagnostics string still says
`deploymentId: isolated-061` / `application_name: tooba-061-isolated`, because the 062 prepare script
was reused verbatim. Tenant/database/role/port provenance above is all 064-specific.

Every value created during the HTTP run is explicitly **synthetic and scratch-only** — not a live
tariff, not coverage policy, not checkout parity evidence.

## Tests

* `src/backend/Modules/Shipping/Tooba.Shipping.Tests/Admin/ShippingAdminInvalidBodyRepairTests.cs` (new,
  7 focused tests): reproduces the historical `BadHttpRequestException → 500 platform.unexpected`
  mapping, proves the declared 400 envelope, and proves the missing-scope code is unchanged.
* `Tooba.Shipping.Tests` full project: **96 passed, 0 failed** (includes the pre-existing 055 route
  ownership guard and the 060 scope-guard repair tests, both unmodified).
* `Tooba.Localization.Infrastructure` and `Tooba.Shipping.Endpoints` build clean (0 warnings, 0 errors).

## Changed paths

| Path | Change |
| --- | --- |
| `.../Tooba.Shipping.Endpoints/Admin/ShippingRateCoverageEndpoints.cs` | manual body read, declared invalid-body envelope, all five write routes |
| `.../Tooba.Shipping.Application/Validation/ShippingAdminValidators.cs` | new declared code `ShippingValidationCodes.BodyInvalid` |
| `.../Tooba.Localization.Infrastructure/Bootstrap/LanguageBootstrapDevelopmentSeed.cs` | new: runner + Development post-migration step |
| `.../Tooba.Localization.Infrastructure/Bootstrap/LanguageBootstrapHostedService.cs` | delegates to runner; 503-at-startup is Information, other failures stay Error |
| `.../Tooba.Localization.Infrastructure/LocalizationModule.cs` | registers the Localization post-migration step |
| `.../Tooba.Shipping.Tests/Admin/ShippingAdminInvalidBodyRepairTests.cs` | new focused regression |

## Evidence index

| File | Content |
| --- | --- |
| `log-guard-effective-target.txt` | 060 guard verdict, resolved tenants/references, config SHA-256 + fingerprint |
| `log-http-064.txt` | full real-HTTP matrix: S1–S9, A1–A4, M1–M3, B1–B2, L1, V1–V4 |
| `log-restart-idempotence.txt` | language rows before/after restart, second-start log, final counts, migration history |
| `log-cleanup.txt` | isolated host stopped, container destroyed, ports free, user host untouched |
| `seed-geography-064.sql`, `seed-id-map-064.txt` | re-generated 047 seed (unmodified generator) applied to scratch |
| `isolated/` | 060 guard + 062 prepare script reused unchanged |
| `probe-http.ps1` | the re-runnable probe script |
| `RESULT.bridge.txt` | the Bridge result envelope |

## Remaining limitations

* `Tooba.Host.Tests` (where the Localization behavior and structure guards live) could not be executed
  in this environment: its build contended with the user's running Development Host (`:5088`,
  PID 37028) over `Tooba.Host/bin/Debug/net8.0/*.dll` and produced no output for over nine minutes, so
  it was stopped without ever reporting a result. The same environmental constraint was recorded in
  062. The Localization change is therefore proven by the two real isolated starts above rather than by
  that suite, and this is the single unverified item in this task. The Localization structure guard
  that asserts `Bootstrap/LanguageBootstrapHostedService.cs` still exists is satisfied by construction —
  the file was kept and refactored, not deleted.
* The malformed-body envelope uses field key `request`; the previously observed field keys for
  *valid* JSON (`Request.Scope`, …) are untouched.
